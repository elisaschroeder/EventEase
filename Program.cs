using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EventEase;
using EventEase.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client Configuration
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Core Services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ISessionTrackingService, SessionTrackingService>();
builder.Services.AddScoped<IStateManagementService, StateManagementService>();
builder.Services.AddScoped<IAttendanceTrackingService, AttendanceTrackingService>();

// Production Readiness Services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IApplicationLogger, ApplicationLogger>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

// Logging Configuration
builder.Logging.SetMinimumLevel(LogLevel.Information);
if (builder.HostEnvironment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

await builder.Build().RunAsync();
