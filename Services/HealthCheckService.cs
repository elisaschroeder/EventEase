using EventEase.Services;
using System.Text.Json;

namespace EventEase.Services
{
    public class HealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public List<HealthCheckResult> Checks { get; set; } = new();
    }

    public class HealthCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public interface IHealthCheckService
    {
        Task<HealthStatus> GetHealthStatusAsync();
        Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName);
        Task<bool> IsSystemHealthyAsync();
    }

    public class HealthCheckService : IHealthCheckService
    {
        private readonly IApplicationLogger _logger;
        private readonly IConfigurationService _configService;
        private readonly IAttendanceTrackingService _attendanceService;
        private readonly IEventService _eventService;

        public HealthCheckService(
            IApplicationLogger logger,
            IConfigurationService configService,
            IAttendanceTrackingService attendanceService,
            IEventService eventService)
        {
            _logger = logger;
            _configService = configService;
            _attendanceService = attendanceService;
            _eventService = eventService;
        }

        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var healthStatus = new HealthStatus();

            try
            {
                await _logger.LogInformationAsync("Starting health check");

                // Run all health checks
                var checks = new List<Task<HealthCheckResult>>
                {
                    CheckServiceHealthAsync("Configuration"),
                    CheckServiceHealthAsync("EventService"),
                    CheckServiceHealthAsync("AttendanceService"),
                    CheckServiceHealthAsync("ApplicationLogger")
                };

                var results = await Task.WhenAll(checks);
                healthStatus.Checks.AddRange(results);

                // Determine overall status
                var failedChecks = results.Where(r => r.Status != "Healthy").ToList();
                if (!failedChecks.Any())
                {
                    healthStatus.Status = "Healthy";
                }
                else if (failedChecks.Any(c => c.Status == "Critical"))
                {
                    healthStatus.Status = "Critical";
                }
                else
                {
                    healthStatus.Status = "Degraded";
                }

                stopwatch.Stop();
                healthStatus.ResponseTime = stopwatch.Elapsed;

                // Add system details
                healthStatus.Details["TotalChecks"] = results.Length;
                healthStatus.Details["FailedChecks"] = failedChecks.Count;
                healthStatus.Details["Environment"] = _configService.IsDevelopmentEnvironment ? "Development" : "Production";
                healthStatus.Details["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

                await _logger.LogInformationAsync("Health check completed: {Status} in {Duration}ms", 
                    healthStatus.Status, healthStatus.ResponseTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                healthStatus.Status = "Critical";
                healthStatus.ResponseTime = stopwatch.Elapsed;
                healthStatus.Details["Error"] = ex.Message;
                
                await _logger.LogErrorAsync(ex, "Health check failed");
            }

            return healthStatus;
        }

        public async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new HealthCheckResult { Name = serviceName };

            try
            {
                switch (serviceName.ToLower())
                {
                    case "configuration":
                        await CheckConfigurationServiceAsync(result);
                        break;
                    case "eventservice":
                        await CheckEventServiceAsync(result);
                        break;
                    case "attendanceservice":
                        await CheckAttendanceServiceAsync(result);
                        break;
                    case "applicationlogger":
                        await CheckApplicationLoggerAsync(result);
                        break;
                    default:
                        result.Status = "Unknown";
                        result.Description = $"Unknown service: {serviceName}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Status = "Critical";
                result.Description = $"Health check failed: {ex.Message}";
                result.Data["Exception"] = ex.GetType().Name;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        public async Task<bool> IsSystemHealthyAsync()
        {
            try
            {
                var healthStatus = await GetHealthStatusAsync();
                return healthStatus.Status == "Healthy";
            }
            catch
            {
                return false;
            }
        }

        private async Task CheckConfigurationServiceAsync(HealthCheckResult result)
        {
            try
            {
                // Test configuration access
                var appName = await _configService.GetSettingAsync("Application:Name", "Unknown");
                var isDev = _configService.IsDevelopmentEnvironment;

                result.Status = "Healthy";
                result.Description = "Configuration service is responsive";
                result.Data["ApplicationName"] = appName;
                result.Data["Environment"] = isDev ? "Development" : "Production";
            }
            catch (Exception ex)
            {
                result.Status = "Critical";
                result.Description = $"Configuration service failed: {ex.Message}";
            }
        }

        private async Task CheckEventServiceAsync(HealthCheckResult result)
        {
            try
            {
                // Test event service by getting events count
                var events = await _eventService.GetEventsAsync();
                var eventCount = events?.Count() ?? 0;

                result.Status = "Healthy";
                result.Description = "Event service is responsive";
                result.Data["EventCount"] = eventCount;
                result.Data["ServiceType"] = _eventService.GetType().Name;
            }
            catch (Exception ex)
            {
                result.Status = "Critical";
                result.Description = $"Event service failed: {ex.Message}";
            }
        }

        private async Task CheckAttendanceServiceAsync(HealthCheckResult result)
        {
            try
            {
                // Test attendance service by getting dashboard data
                var dashboardData = await _attendanceService.GetAttendanceDashboardDataAsync();

                result.Status = "Healthy";
                result.Description = "Attendance service is responsive";
                result.Data["DashboardKeys"] = dashboardData?.Keys.Count ?? 0;
                result.Data["ServiceType"] = _attendanceService.GetType().Name;
                
                if (dashboardData?.ContainsKey("TodayCheckIns") == true)
                {
                    result.Data["TodayCheckIns"] = dashboardData["TodayCheckIns"];
                }
                if (dashboardData?.ContainsKey("TotalAttendees") == true)
                {
                    result.Data["TotalAttendees"] = dashboardData["TotalAttendees"];
                }
            }
            catch (Exception ex)
            {
                result.Status = "Critical";
                result.Description = $"Attendance service failed: {ex.Message}";
            }
        }

        private async Task CheckApplicationLoggerAsync(HealthCheckResult result)
        {
            try
            {
                // Test logger by writing a test log entry
                await _logger.LogDebugAsync("Health check test log entry");

                result.Status = "Healthy";
                result.Description = "Application logger is responsive";
                result.Data["LoggerType"] = _logger.GetType().Name;
            }
            catch (Exception ex)
            {
                result.Status = "Degraded";
                result.Description = $"Application logger issue: {ex.Message}";
            }
        }
    }
}