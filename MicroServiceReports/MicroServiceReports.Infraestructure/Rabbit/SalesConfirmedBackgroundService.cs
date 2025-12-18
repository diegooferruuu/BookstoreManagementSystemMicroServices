using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceReports.Domain.Models;
using MicroServiceReports.Domain.Ports;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroServiceReports.Infraestructure.Rabbit
{
    public class RabbitSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Exchange { get; set; } = "ventas.events";
        public string RoutingKey { get; set; } = "venta.confirmada";
        public string Queue { get; set; } = "ventas.confirmadas.queue";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }

    public class SalesConfirmedBackgroundService : BackgroundService
    {
        private readonly ILogger<SalesConfirmedBackgroundService> _logger;
        private readonly RabbitSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private RabbitMQ.Client.IChannel? _channel;
        private RabbitMQ.Client.IConnection? _connection;

        public SalesConfirmedBackgroundService(ILogger<SalesConfirmedBackgroundService> logger, IOptions<RabbitSettings> options, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _settings = options.Value;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectAndStartConsumerAsync(stoppingToken);
        }

        private async Task ConnectAndStartConsumerAsync(CancellationToken stoppingToken)
        {
            var factory = new RabbitMQ.Client.ConnectionFactory()
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare queue/bindings is optional if already created, but harmless.
            await _channel.ExchangeDeclareAsync(_settings.Exchange, ExchangeType.Topic, durable: true);
            await _channel.QueueDeclareAsync(_settings.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            await _channel.QueueBindAsync(_settings.Queue, _settings.Exchange, _settings.RoutingKey);

            var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(_channel);
            // Subscribe to ReceivedAsync (async handler)
            consumer.ReceivedAsync += async (object sender, RabbitMQ.Client.Events.BasicDeliverEventArgs ea) =>
            {
                await HandleMessageAsync(ea).ConfigureAwait(false);
            };

            // BasicConsume(queue, autoAck, consumer)
            await _channel.BasicConsumeAsync(_settings.Queue, false, consumer);

            _logger.LogInformation("RabbitMQ consumer started on queue {Queue}", _settings.Queue);
        }

        private async Task HandleMessageAsync(RabbitMQ.Client.Events.BasicDeliverEventArgs ea)
        {
            var bodyBytes = ea.Body.ToArray();
            var payload = Encoding.UTF8.GetString(bodyBytes, 0, bodyBytes.Length);
            long saleId = 0;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("saleId", out var saleIdProp) && saleIdProp.ValueKind == JsonValueKind.Number)
                {
                    saleId = saleIdProp.GetInt64();
                }
                else
                {
                    _logger.LogWarning("Received message without saleId, will nack and not requeue. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                    // Permanent bad message - reject without requeue (send to DLX if configured)
                    if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "Failed to parse incoming message JSON. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetService<ISaleEventRepository>();
                if (repo == null)
                {
                    _logger.LogError("ISaleEventRepository not registered. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                    if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    return;
                }

                var record = new SaleEventRecord
                {
                    Id = Guid.NewGuid(),
                    SaleId = saleId,
                    Payload = payload,
                    Exchange = ea.Exchange,
                    RoutingKey = ea.RoutingKey,
                    ReceivedAt = DateTime.UtcNow
                };

                await repo.SaveAsync(record).ConfigureAwait(false);

                // ACK only after successful save
                if (_channel != null) await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Message persisted and ACKed. SaleId={SaleId}, DeliveryTag={DeliveryTag}", saleId, ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist message. DeliveryTag={DeliveryTag}, SaleId={SaleId}", ea.DeliveryTag, saleId);
                // Transient error policy: NACK with requeue true to retry later
                try
                {
                    if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "Failed to NACK message. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                // Prefer safe close on channel/connection; swallow exceptions but log
                try
                {
                    if (_channel != null)
                    {
                        try
                        {
                            _channel.CloseAsync().GetAwaiter().GetResult();
                        }
                        catch
                        {
                            // ignore close exceptions
                        }

                        _channel.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                }
                catch (Exception cex)
                {
                    _logger.LogWarning(cex, "Error while closing channel");
                }

                try
                {
                    if (_connection != null)
                    {
                        try
                        {
                            _connection.CloseAsync().GetAwaiter().GetResult();
                        }
                        catch
                        {
                            // ignore
                        }

                        _connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                }
                catch (Exception conex)
                {
                    _logger.LogWarning(conex, "Error while closing connection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disposing RabbitMQ connection/channel");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
