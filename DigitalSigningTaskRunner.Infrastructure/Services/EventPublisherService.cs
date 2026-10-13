using DigitalSigningTaskRunner.Core.Constants.Messages;
using DigitalSigningTaskRunner.Core.Constants.TaskRunner;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace DigitalSigningTaskRunner.Infrastructure.Services;

public sealed class EventPublisherService : IEventPublisherService, IAsyncDisposable
{
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly ILoggingService<EventPublisherService> _logger;

    // Mantendremos la conexión y el canal vivos durante el ciclo de vida del servicio
    private IConnection? _connection;
    private IChannel? _channel;

    public EventPublisherService(IOptions<RabbitMQSettings> rabbitOptions, ILoggingService<EventPublisherService> logger)
    {
        _rabbitSettings = rabbitOptions.Value;
        _logger = logger;
    }

    private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        // Si ya están creados y abiertos, no hacemos nada
        if (_channel is not null && _connection is not null) return;

        var factory = new ConnectionFactory
        {
            HostName = _rabbitSettings.Host,
            Port = _rabbitSettings.Port,
            VirtualHost = _rabbitSettings.VirtualHost,
            UserName = _rabbitSettings.Username,
            Password = _rabbitSettings.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // 1. Declarar el Exchange (Tipo Direct)
        await _channel.ExchangeDeclareAsync(
            exchange: _rabbitSettings.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        // 2. Declarar la Cola usando la propiedad del JSON
        await _channel.QueueDeclareAsync(
            queue: _rabbitSettings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        // 3. Unir la Cola con el Exchange mediante la RoutingKey (Binding)
        await _channel.QueueBindAsync(
            queue: _rabbitSettings.QueueName,
            exchange: _rabbitSettings.ExchangeName,
            routingKey: _rabbitSettings.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken
        );
    }

    public async Task<bool> PublishAsync<T>(T eventMessage, CancellationToken cancellationToken = default) where T : class
    {
        if (eventMessage == null)
        {
            _logger.LogError(null, LogsConstants.ERROR_INVALID_DATA_FORMAT);
            return false;
        }

        try
        {
            // Garantizamos que la conexión está viva antes de publicar
            await InitializeRabbitMqAsync(cancellationToken);

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(eventMessage);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = SettingsConstants.HTTP_CONTENT_TYPE_JSON
            };

            await _channel!.BasicPublishAsync(
                exchange: _rabbitSettings.ExchangeName,
                routingKey: _rabbitSettings.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogsConstants.ERROR_RABBITMQ_PUBLISH, ex.Message);
            return false;
        }
    }

    // Liberación segura de recursos al apagar el Worker
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}