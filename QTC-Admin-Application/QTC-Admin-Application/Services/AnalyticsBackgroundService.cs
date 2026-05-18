using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QTC_Admin_Application.Services;

namespace QTC_Admin_Application.Services
{
    public class AnalyticsBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnalyticsBackgroundService> _logger;

        public AnalyticsBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AnalyticsBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analytics background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var workflowService = scope.ServiceProvider.GetRequiredService<WorkflowService>();

                        var workflows = await workflowService.GetAllAsync();
                        var totalWorkflows = workflows.Count;

                        _logger.LogInformation(
                            "Analytics refreshed at {Time} — Total Workflows: {TotalWorkflows}",
                            DateTime.Now, totalWorkflows
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during analytics refresh.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}