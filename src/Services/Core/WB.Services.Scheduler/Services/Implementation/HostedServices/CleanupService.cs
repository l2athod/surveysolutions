﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WB.Services.Infrastructure.Logging;

namespace WB.Services.Scheduler.Services.Implementation.HostedServices
{
    internal class CleanupService : IHostedSchedulerService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<JobSettings> options;
        private readonly ILogger<CleanupService> logger;
        private Task? task;

        public CleanupService(IServiceProvider serviceProvider, IOptions<JobSettings> options, ILogger<CleanupService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.options = options;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.task = Task.Run(async () =>
            {
                using var ctx = LoggingHelpers.LogContext("workerId", "Cleanup service");
                logger.LogTrace("Start tracking of stale running jobs");

                while (!cancellationToken.IsCancellationRequested)
                { 
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        using (var scope = this.serviceProvider.CreateScope())
                        {
                            var cleanup = scope.ServiceProvider.GetRequiredService<StaleJobCleanupService>();
                            await cleanup.ExecuteAsync(cancellationToken);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(options.Value.ClearStaleJobsInSeconds),
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("CancellationToken cancel request received.");
                    }
                    catch (Exception e)
                    {
                        logger.LogError("Error while executing cleanup service", e);
                    }
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (task != null) await task;
            } catch { /* om om om: we just need to wait for task to complete */}
        }
    }
}
