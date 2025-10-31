using Microsoft.Extensions.Hosting;
using Vostok.Logging.Abstractions;

namespace Tolltech.Planny;

public class PlanUpdater(ILog log, PlannyJobRunner plannyJobRunner) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.Info("PlanUpdater service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                log.Info("PlanUpdater task running");

                var newPlans = await plannyJobRunner.Run();

                log.Info($"Run {newPlans} new plans");
                
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error in PlanUpdater service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        log.Info("PlanUpdater service stopping...");
    }
}