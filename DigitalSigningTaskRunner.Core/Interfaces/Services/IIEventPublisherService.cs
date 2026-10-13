namespace DigitalSigningTaskRunner.Core.Interfaces.Services;

public interface IEventPublisherService
{
    /// <summary>
    /// Publica de forma asíncrona un evento estructurado en RabbitMQ.
    /// Retorna verdadero si se publicó correctamente, o falso si ocurrió un error (registrado en el log).
    /// </summary>
    Task<bool> PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class;
}