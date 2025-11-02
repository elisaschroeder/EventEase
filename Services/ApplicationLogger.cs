using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IApplicationLogger
    {
        Task LogInformationAsync(string message, params object[] args);
        Task LogWarningAsync(string message, params object[] args);
        Task LogErrorAsync(Exception exception, string message, params object[] args);
        Task LogDebugAsync(string message, params object[] args);
        Task LogUserActionAsync(string action, string details, string? userId = null);
        Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? metadata = null);
    }

    public class ApplicationLogger : IApplicationLogger
    {
        private readonly ILogger<ApplicationLogger> _logger;
        private readonly IConfigurationService _configService;
        private readonly Queue<LogEntry> _logBuffer;
        private readonly SemaphoreSlim _bufferLock;
        private readonly Timer _flushTimer;

        public ApplicationLogger(ILogger<ApplicationLogger> logger, IConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;
            _logBuffer = new Queue<LogEntry>();
            _bufferLock = new SemaphoreSlim(1, 1);
            
            // Flush logs every 30 seconds
            _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task LogInformationAsync(string message, params object[] args)
        {
            await LogAsync(LogLevel.Information, message, null, args);
        }

        public async Task LogWarningAsync(string message, params object[] args)
        {
            await LogAsync(LogLevel.Warning, message, null, args);
        }

        public async Task LogErrorAsync(Exception exception, string message, params object[] args)
        {
            await LogAsync(LogLevel.Error, message, exception, args);
        }

        public async Task LogDebugAsync(string message, params object[] args)
        {
            if (_configService.IsDevelopmentEnvironment)
            {
                await LogAsync(LogLevel.Debug, message, null, args);
            }
        }

        public async Task LogUserActionAsync(string action, string details, string? userId = null)
        {
            var auditEnabled = await _configService.GetBooleanSettingAsync("Security:EnableAuditLogging", true);
            if (auditEnabled)
            {
                var auditLog = new
                {
                    Action = action,
                    Details = details,
                    UserId = userId ?? "Anonymous",
                    Timestamp = DateTime.UtcNow,
                    Type = "UserAction"
                };

                await LogAsync(LogLevel.Information, "User Action: {Action} - {Details}", null, action, details);
            }
        }

        public async Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? metadata = null)
        {
            var performanceLog = new
            {
                Operation = operation,
                DurationMs = duration.TotalMilliseconds,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                Type = "Performance"
            };

            var logMessage = $"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms";
            if (metadata?.Any() == true)
            {
                logMessage += $" - Metadata: {JsonSerializer.Serialize(metadata)}";
            }

            await LogAsync(LogLevel.Information, logMessage, null);
        }

        private async Task LogAsync(LogLevel level, string message, Exception? exception, params object[] args)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Level = level,
                    Message = string.Format(message, args),
                    Exception = exception,
                    Timestamp = DateTime.UtcNow,
                    Source = "EventEase"
                };

                // Add to buffer for batch processing
                await _bufferLock.WaitAsync();
                try
                {
                    _logBuffer.Enqueue(logEntry);
                    
                    // Immediate flush for errors
                    if (level == LogLevel.Error || level == LogLevel.Critical)
                    {
                        await FlushLogsAsync();
                    }
                }
                finally
                {
                    _bufferLock.Release();
                }

                // Also log immediately to console/debug for development
                if (_configService.IsDevelopmentEnvironment)
                {
                    if (exception != null)
                    {
                        _logger.Log(level, exception, message, args);
                    }
                    else
                    {
                        _logger.Log(level, message, args);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback logging
                _logger.LogError(ex, "Error in ApplicationLogger.LogAsync");
            }
        }

        private async void FlushLogs(object? state)
        {
            await FlushLogsAsync();
        }

        private async Task FlushLogsAsync()
        {
            if (!_logBuffer.Any()) return;

            await _bufferLock.WaitAsync();
            try
            {
                var logsToFlush = new List<LogEntry>();
                while (_logBuffer.Count > 0)
                {
                    logsToFlush.Add(_logBuffer.Dequeue());
                }

                // Process logs (in production, this would send to external logging service)
                foreach (var logEntry in logsToFlush)
                {
                    if (logEntry.Exception != null)
                    {
                        _logger.Log(logEntry.Level, logEntry.Exception, logEntry.Message);
                    }
                    else
                    {
                        _logger.Log(logEntry.Level, logEntry.Message);
                    }
                }
            }
            finally
            {
                _bufferLock.Release();
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushLogsAsync().Wait();
            _bufferLock?.Dispose();
        }
    }

    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}