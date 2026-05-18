using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace QTC_Admin_Application.Services
{
    public class LoginMonitorBackgroundService : BackgroundService
    {
        private readonly LoginMonitorService _monitor;
        private readonly ILogger<LoginMonitorBackgroundService> _logger;
        private readonly int _intervalMinutes;

        public LoginMonitorBackgroundService(
            LoginMonitorService monitor,
            ILogger<LoginMonitorBackgroundService> logger,
            IConfiguration config)
        {
            _monitor = monitor;
            _logger = logger;
            _intervalMinutes = config.GetValue<int>("Monitoring:IntervalMinutes", 5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Login Monitor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string result = await _monitor.RunLoginScan();

                    using var doc = JsonDocument.Parse(result);
                    var root = doc.RootElement;

                    Console.WriteLine("\n========== LOGIN MONITOR SCAN ==========");

                    // Extract values first to avoid nested-quote interpolation issues
                    var timestamp = root.GetProperty("timestamp").GetString() ?? "-";
                    var cpuUsage = root.GetProperty("cpu_usage").GetSingle();
                    var memoryUsage = root.GetProperty("memory_usage").GetSingle();
                    var apiStatus = root.GetProperty("api_status").GetString() ?? "-";

                    Console.WriteLine($"Timestamp     : {timestamp}");
                    Console.WriteLine($"CPU Usage     : {cpuUsage:F2}%");
                    Console.WriteLine($"Memory Usage  : {memoryUsage:F2}%");
                    Console.WriteLine($"API Status    : {apiStatus}");

                    int logins = root.GetProperty("logins_last_hour").GetInt32();
                    int failed = root.GetProperty("failed_logins").GetInt32();
                    int successful = logins - failed;

                    Console.WriteLine($"\n--- Login Activity (Last Hour) ---");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Successful Logins : {successful}");
                    Console.ResetColor();

                    Console.ForegroundColor = failed > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
                    Console.WriteLine($"  Failed Logins     : {failed}");
                    Console.ResetColor();

                    var alerts = root.GetProperty("alerts");
                    if (alerts.ValueKind == JsonValueKind.Array && alerts.GetArrayLength() == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n  No alerts. All systems normal.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n--- Alerts ---");
                        foreach (var alert in alerts.EnumerateArray())
                        {
                            Console.WriteLine($"  ⚠  {alert.GetString()}");
                        }
                        Console.ResetColor();
                    }

                    Console.WriteLine("=========================================\n");

                    _logger.LogInformation("Scan complete. Logins: {L}, Failed: {F}", logins, failed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Login monitor scan failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
        }
    }
}