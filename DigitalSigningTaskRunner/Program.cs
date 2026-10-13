using DigitalSigningTaskRunner;
using DigitalSigningTaskRunner.Application.Configuration;
using DigitalSigningTaskRunner.Core.Constants.TaskRunner;
using DigitalSigningTaskRunner.Infrastructure.Persistence;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // 🟢 Host en lugar de WebApplication (Necesario para Workers)
        var builder = Host.CreateApplicationBuilder(args);

        // 🟢 Integración con el Gestor de Servicios de Windows usando tu constante
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = SettingsConstants.WINDOWS_SERVICE_NAME;
        });

        // Extensiones de builder
        builder.AddLogs();
        builder.AddTaskRunner();
        builder.AddDataBase();

        // 🟢 El motor principal que enciende el ciclo de vida del Worker
        builder.Services.AddHostedService<HostedService>();

        var host = builder.Build();

        await host.RunAsync();
    }
}