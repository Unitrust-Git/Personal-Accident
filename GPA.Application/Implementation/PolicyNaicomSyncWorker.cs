using GPA.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GPA.Application.Implementation
{
    public class PolicyNaicomSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PolicyNaicomSyncWorker> _logger;

        private static readonly TimeSpan PollDelay = TimeSpan.FromMinutes(5);

        public PolicyNaicomSyncWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<PolicyNaicomSyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Policy NAICOM sync worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var syncService = scope.ServiceProvider
                        .GetRequiredService<IPolicyNaicomSyncService>();

                    await syncService.SyncMissingNaicomIdsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Policy NAICOM sync worker failed.");
                }

                try
                {
                    await Task.Delay(PollDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("Policy NAICOM sync worker stopped.");
        }
    }
}