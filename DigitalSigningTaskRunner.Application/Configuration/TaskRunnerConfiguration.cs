using DigitalSigningTaskRunner.Core.Constants.TaskRunner;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using DigitalSigningTaskRunner.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace DigitalSigningTaskRunner.Application.Configuration;

public static class TaskRunnerConfiguration
{
    public static TBuilder AddTaskRunner<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // Habilita codificaciones de páginas de códigos (ANSI / Windows-1252) para PDF
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        AddSettings(builder.Services, builder.Configuration);
        AddServices(builder.Services);

        return builder;
    }

    private static void AddServices(IServiceCollection services)
    {
        // 1. Trazabilidad
        services.AddSingleton(typeof(ILoggingService<>), typeof(LoggingService<>));

        // 2. Infraestructura NAS / Archivos
        services.AddSingleton<INetworkConnectionService, NetworkConnectionService>();
        services.AddSingleton<IStorageService, StorageService>();

        // 3. Procesamiento y Firma (Cliente HTTP Tipado)
        services.AddHttpClient<IDigitalSignatureService, DigitalSignatureService>();

        // 4. Mensajería (RabbitMQ)
        services.AddSingleton<IEventPublisherService, EventPublisherService>();

        // 5. CQRS (MediatR)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
    }

    private static void AddSettings(IServiceCollection services, IConfiguration configuration)
    {
        // Mapeo estricto de todas las secciones del appsettings.json
        services.Configure<TaskRunnerSettings>(configuration.GetSection(SettingsConstants.TASK_RUNNER_SETTINGS_SECTION_NAME));
        services.Configure<PriorityWordsSettings>(configuration.GetSection(SettingsConstants.PRIORITY_WORDS_SETTINGS_SECTION_NAME));
        services.Configure<NASSettings>(configuration.GetSection(SettingsConstants.NAS_SETTINGS_SECTION_NAME));
        services.Configure<SigningApiSettings>(configuration.GetSection(SettingsConstants.SIGNING_API_SETTINGS_SECTION_NAME));
        services.Configure<RabbitMQSettings>(configuration.GetSection(SettingsConstants.RABBITMQ_SETTINGS_SECTION_NAME));
    }
}