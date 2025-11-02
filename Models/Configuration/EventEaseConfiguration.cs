using System.Text.Json;

namespace EventEase.Models.Configuration
{
    public class ApplicationSettings
    {
        public string Name { get; set; } = "EventEase";
        public string Version { get; set; } = "1.0.0";
        public string Environment { get; set; } = "Development";
    }

    public class FeatureSettings
    {
        public bool EnableSampleData { get; set; } = true;
        public bool EnableDiagnostics { get; set; } = true;
        public int MaxAttendeesPerEvent { get; set; } = 1000;
        public int ReportRetentionDays { get; set; } = 365;
    }

    public class PerformanceSettings
    {
        public int CacheTimeoutMinutes { get; set; } = 30;
        public int MaxConcurrentOperations { get; set; } = 10;
        public int DatabaseTimeoutSeconds { get; set; } = 30;
    }

    public class SecuritySettings
    {
        public bool RequireAuthentication { get; set; } = false;
        public bool EnableAuditLogging { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 60;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public bool EnableRetry { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class MonitoringSettings
    {
        public string ApplicationInsightsKey { get; set; } = string.Empty;
        public bool EnableHealthChecks { get; set; } = true;
        public int HealthCheckIntervalSeconds { get; set; } = 30;
    }

    public class EventEaseConfiguration
    {
        public ApplicationSettings Application { get; set; } = new();
        public FeatureSettings Features { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public MonitoringSettings Monitoring { get; set; } = new();
    }
}