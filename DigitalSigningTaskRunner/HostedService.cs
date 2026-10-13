using DigitalSigningTaskRunner.Application.UseCases;
using DigitalSigningTaskRunner.Core.Constants.Messages;
using DigitalSigningTaskRunner.Core.Constants.TaskRunner;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace DigitalSigningTaskRunner;

public class HostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TaskRunnerSettings> taskRunnerOptions,
    ILoggingService<HostedService> logger) : BackgroundService
{
    private readonly TaskRunnerSettings _settings = taskRunnerOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInfo(LogsConstants.INFO_WORKER_STARTING);
        logger.LogInfo(LogsConstants.INFO_AUTHOR);
        logger.LogInfo(LogsConstants.INFO_CONTACT);

        int intervalSeconds = _settings.IntervalInSeconds > 0
            ? _settings.IntervalInSeconds
            : SettingsConstants.DEFAULT_INTERVAL_IN_SECONDS;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 🟢 REGLA DE ORO: Crear un Scope por cada iteración.
                using (IServiceScope scope = serviceScopeFactory.CreateScope())
                {
                    ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

                    // Disparamos el Orquestador
                    await sender.Send(new ProcessPriorityDocsCommand(), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInfo(LogsConstants.INFO_WORKER_STOP_SIGNAL);
                break;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, LogsConstants.ERROR_WORKER_INTERNAL_CANCELLED, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, LogsConstants.ERROR_WORKER_CRITICAL, ex.Message);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInfo(LogsConstants.INFO_WORKER_STOP_SIGNAL);
                    break;
                }
            }
        }

        logger.LogInfo(LogsConstants.INFO_WORKER_STOPPED);
    }
}