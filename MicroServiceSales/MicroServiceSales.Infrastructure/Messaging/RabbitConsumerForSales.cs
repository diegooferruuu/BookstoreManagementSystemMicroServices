using MicroServiceSales.Application.Services;
using MicroServiceSales.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MicroServiceSales.Infrastructure.Messaging
{
    public class RabbitConsumerForSales : BackgroundService
    {
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly ILogger<RabbitConsumerForSales> _log;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitConsumerForSales(
            IConfiguration cfg,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitConsumerForSales> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;

            var factory = new ConnectionFactory
            {
                HostName = cfg["RabbitMQ:Host"] ?? "localhost",
                UserName = cfg["RabbitMQ:User"] ?? "guest",
                Password = cfg["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            // Exchange por defecto para eventos de ventas
            _exchange = cfg["RabbitMQ:Exchange"] ?? "sales.events";

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);

            // Cola enfocada a procesos de ventas
            _channel.QueueDeclare("sales.queue", durable: true, exclusive: false, autoDelete: false);
            // Escuchamos la confirmación de stock desde el microservicio de productos
            _channel.QueueBind("sales.queue", _exchange, "sales.confirmed");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Use AsyncEventingBasicConsumer in v6.x
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceived;
            _channel.BasicConsume("sales.queue", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task OnReceived(object sender, BasicDeliverEventArgs ea)
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Guid saleId = root.GetProperty("SaleId").GetGuid();

                _log.LogInformation("Procesando sales.confirmed: saleId={saleId}", saleId);

                using var scope = _scopeFactory.CreateScope();
                var salesService = scope.ServiceProvider.GetRequiredService<SalesService>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                try
                {
                    var sale = salesService.Read(saleId);
                    if (sale is null)
                        throw new InvalidOperationException($"Sale {saleId} not found");

                    // Confirmar la venta cambiando su estado a COMPLETED
                    sale.Status = "COMPLETED";
                    salesService.Update(sale);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error confirmando la venta");
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error procesando mensaje sales.confirmed");
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _conn?.Dispose();
            base.Dispose();
        }
    }
}
