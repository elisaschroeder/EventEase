using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IConfigurationService
    {
        Task<T> GetConfigurationAsync<T>(string key) where T : class, new();
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task<bool> GetBooleanSettingAsync(string key, bool defaultValue = false);
        Task<int> GetIntegerSettingAsync(string key, int defaultValue = 0);
        bool IsProductionEnvironment { get; }
        bool IsDevelopmentEnvironment { get; }
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly Dictionary<string, object> _configCache;
        private readonly SemaphoreSlim _cacheLock;
        private DateTime _lastCacheUpdate;

        public ConfigurationService(HttpClient httpClient, ILogger<ConfigurationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configCache = new Dictionary<string, object>();
            _cacheLock = new SemaphoreSlim(1, 1);
            _lastCacheUpdate = DateTime.MinValue;
        }

        public bool IsProductionEnvironment => 
            GetSettingAsync("Application:Environment").Result.Equals("Production", StringComparison.OrdinalIgnoreCase);

        public bool IsDevelopmentEnvironment => 
            GetSettingAsync("Application:Environment").Result.Equals("Development", StringComparison.OrdinalIgnoreCase);

        public async Task<T> GetConfigurationAsync<T>(string key) where T : class, new()
        {
            try
            {
                await EnsureConfigurationLoadedAsync();
                
                if (_configCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
                {
                    return typedValue;
                }

                _logger.LogWarning("Configuration key '{Key}' not found, returning default instance", key);
                return new T();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration for key '{Key}'", key);
                return new T();
            }
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            try
            {
                await EnsureConfigurationLoadedAsync();
                
                var keyParts = key.Split(':');
                object? current = _configCache.Values.FirstOrDefault();
                
                if (current != null)
                {
                    foreach (var part in keyParts)
                    {
                        if (current is JsonElement element)
                        {
                            if (element.TryGetProperty(part, out var property))
                            {
                                current = property;
                            }
                            else
                            {
                                return defaultValue;
                            }
                        }
                        else
                        {
                            return defaultValue;
                        }
                    }
                    
                    if (current is JsonElement finalElement)
                    {
                        return finalElement.GetString() ?? defaultValue;
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting '{Key}'", key);
                return defaultValue;
            }
        }

        public async Task<bool> GetBooleanSettingAsync(string key, bool defaultValue = false)
        {
            var stringValue = await GetSettingAsync(key, defaultValue.ToString());
            return bool.TryParse(stringValue, out var result) ? result : defaultValue;
        }

        public async Task<int> GetIntegerSettingAsync(string key, int defaultValue = 0)
        {
            var stringValue = await GetSettingAsync(key, defaultValue.ToString());
            return int.TryParse(stringValue, out var result) ? result : defaultValue;
        }

        private async Task EnsureConfigurationLoadedAsync()
        {
            if (DateTime.UtcNow - _lastCacheUpdate < TimeSpan.FromMinutes(5) && _configCache.Any())
            {
                return;
            }

            await _cacheLock.WaitAsync();
            try
            {
                if (DateTime.UtcNow - _lastCacheUpdate < TimeSpan.FromMinutes(5) && _configCache.Any())
                {
                    return;
                }

                await LoadConfigurationAsync();
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                _logger.LogInformation("Loading application configuration");

                // Try to load production configuration first, then fall back to default
                var configFiles = new[] { "appsettings.Production.json", "appsettings.json" };
                
                foreach (var configFile in configFiles)
                {
                    try
                    {
                        var configJson = await _httpClient.GetStringAsync(configFile);
                        var config = JsonSerializer.Deserialize<JsonElement>(configJson);
                        
                        _configCache.Clear();
                        _configCache["root"] = config;
                        _lastCacheUpdate = DateTime.UtcNow;
                        
                        _logger.LogInformation("Configuration loaded successfully from {ConfigFile}", configFile);
                        return;
                    }
                    catch (HttpRequestException)
                    {
                        _logger.LogDebug("Configuration file {ConfigFile} not found, trying next", configFile);
                        continue;
                    }
                }

                // If no config files found, use defaults
                _logger.LogWarning("No configuration files found, using default settings");
                LoadDefaultConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration, using defaults");
                LoadDefaultConfiguration();
            }
        }

        private void LoadDefaultConfiguration()
        {
            var defaultConfig = new
            {
                Application = new { Name = "EventEase", Version = "1.0.0", Environment = "Development" },
                Features = new { EnableSampleData = true, EnableDiagnostics = true, MaxAttendeesPerEvent = 1000, ReportRetentionDays = 365 },
                Performance = new { CacheTimeoutMinutes = 30, MaxConcurrentOperations = 10, DatabaseTimeoutSeconds = 30 },
                Security = new { RequireAuthentication = false, EnableAuditLogging = true, SessionTimeoutMinutes = 60 }
            };

            var jsonString = JsonSerializer.Serialize(defaultConfig);
            var config = JsonSerializer.Deserialize<JsonElement>(jsonString);
            
            _configCache.Clear();
            _configCache["root"] = config;
            _lastCacheUpdate = DateTime.UtcNow;
        }
    }
}