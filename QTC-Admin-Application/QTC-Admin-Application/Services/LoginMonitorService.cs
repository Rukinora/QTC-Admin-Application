using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace QTC_Admin_Application.Services
{
    public class LoginMonitorService
    {
        private readonly IConfiguration _config;

        public LoginMonitorService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> RunLoginScan()
        {
            int loginCount = 0;
            int failedLogins = 0;

            bool databaseConnectionError = false;
            bool authenticationQueryError = false;
            bool slowQueryDetected = false;

            List<string> alerts = new List<string>();

            DateTime oneHourAgo = DateTime.Now.AddHours(-1);
            string? connectionString = _config.GetConnectionString("DefaultConnection");

            // thresholds from config
            int cpuThreshold = _config.GetValue<int>("Monitoring:CpuThreshold", 80);
            int memoryThreshold = _config.GetValue<int>("Monitoring:MemoryThreshold", 75);
            int failedLoginThreshold = _config.GetValue<int>("Monitoring:FailedLoginThreshold", 5);
            string? apiUrl = _config["Monitoring:ApiUrl"];

            // system metrics
            float cpuUsage = GetCpuUsage();
            float memoryUsage = GetMemoryUsage();

            // API check
            bool apiUp = await CheckApi(apiUrl);

            try
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    databaseConnectionError = true;
                    alerts.Add("ALERT: Database connection string is not configured.");
                }
                else
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();

                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        if (conn.State != System.Data.ConnectionState.Open)
                        {
                            databaseConnectionError = true;
                            alerts.Add("ALERT: Database connection could not be established.");
                        }

                        string queryLogins = @"
                            SELECT COUNT(*)
                            FROM Logins
                            WHERE LoginTime > @time";

                        using (SqlCommand cmd = new SqlCommand(queryLogins, conn))
                        {
                            cmd.Parameters.AddWithValue("@time", oneHourAgo);
                            loginCount = (int)cmd.ExecuteScalar();
                        }

                        string queryFailed = @"
                            SELECT COUNT(*)
                            FROM Logins
                            WHERE LoginTime > @time AND Status = 'Failed'";

                        using (SqlCommand cmd = new SqlCommand(queryFailed, conn))
                        {
                            cmd.Parameters.AddWithValue("@time", oneHourAgo);
                            failedLogins = (int)cmd.ExecuteScalar();
                        }
                    }

                    timer.Stop();

                    if (timer.ElapsedMilliseconds > 2000)
                    {
                        slowQueryDetected = true;
                        alerts.Add("ALERT: Authentication database query is slow (>2 seconds).");
                    }
                }
            }
            catch (SqlException)
            {
                databaseConnectionError = true;
                alerts.Add("ALERT: Database connection failure detected.");
            }
            catch (Exception)
            {
                authenticationQueryError = true;
                alerts.Add("ALERT: Backend authentication service error detected.");
            }

            // 🔥 EXISTING + IMPROVED ALERTS

            if (loginCount == 0)
                alerts.Add("ALERT: No logins detected in the last hour.");

            if (failedLogins > failedLoginThreshold)
                alerts.Add($"ALERT: High number of failed logins detected ({failedLogins}).");

            // 🔥 NEW: system alerts

            if (cpuUsage > cpuThreshold)
                alerts.Add($"ALERT: High CPU usage detected ({cpuUsage:F2}%).");

            if (memoryUsage > memoryThreshold)
                alerts.Add($"ALERT: High memory usage detected ({memoryUsage:F2}%).");

            if (!apiUp)
                alerts.Add("ALERT: API is DOWN or unreachable.");

            // 🔥 FINAL OUTPUT

            var output = new
            {
                timestamp = DateTime.Now.ToString(),
                cpu_usage = cpuUsage,
                memory_usage = memoryUsage,
                api_status = apiUp ? "UP" : "DOWN",
                logins_last_hour = loginCount,
                failed_logins = failedLogins,
                database_connection_error = databaseConnectionError,
                authentication_error = authenticationQueryError,
                slow_query_detected = slowQueryDetected,
                alerts = alerts
            };

            string json = JsonSerializer.Serialize(output, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText("system_health.json", json);

            return json;
        }

        // 🔥 NEW METHODS

        // Use process-based CPU measurement (cross-platform) — measures current process CPU %
        private float GetCpuUsage()
        {
            var proc = Process.GetCurrentProcess();
            var startCpu = proc.TotalProcessorTime;
            var sw = Stopwatch.StartNew();
            Thread.Sleep(500);
            sw.Stop();
            proc.Refresh();
            var endCpu = proc.TotalProcessorTime;

            double cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
            double cpuTotalMs = sw.ElapsedMilliseconds * Environment.ProcessorCount;
            if (cpuTotalMs <= 0) return 0f;
            double cpuPercent = (cpuUsedMs / cpuTotalMs) * 100.0;
            return (float)cpuPercent;
        }

        // Approximate memory usage as current process working set vs. GC-provided available memory
        private float GetMemoryUsage()
        {
            long used = Process.GetCurrentProcess().WorkingSet64;
            long avail = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (avail <= 0) return 0f;
            return (float)(used * 100.0 / avail);
        }

        private async Task<bool> CheckApi(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true; // skip if not set

            try
            {
                using HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}