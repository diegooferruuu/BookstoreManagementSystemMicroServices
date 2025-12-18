using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MicroServiceProduct.Application.Services;
using MicroServiceProduct.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroServiceProduct.Infraestructure.Messaging
{
    public class RabbitConsumerForProduct : BackgroundService
    {
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly ILogger<RabbitConsumerForProduct> _log;
        private readonly IServiceScopeFactory _scopeFactory;

        public RabbitConsumerForProduct(
            IConfiguration cfg,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitConsumerForProduct> log)
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

            _exchange = cfg["RabbitMQ:Exchange"] ?? "sales.events";

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);

            _channel.QueueDeclare("products.queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("products.queue", _exchange, "sales.created");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceived;
            _channel.BasicConsume("products.queue", autoAck: true, consumer: consumer);
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
                var itemsElem = root.GetProperty("Items");
                var items = new Dictionary<Guid, int>();
                foreach (var item in itemsElem.EnumerateArray())
                {
                    var pid = item.GetProperty("ProductId").GetGuid();
                    var qty = item.GetProperty("Quantity").GetInt32();
                    items[pid] = qty;
                }

                _log.LogInformation("Procesando sales.created: saleId={saleId} items={count}", saleId, items.Count);

                using var scope = _scopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                if (productService.TryReserveStock(items, out var error))
                {
                    await publisher.PublishAsync("sales.confirmed", new { SaleId = saleId, Status = "CONFIRMED" });
                }
                else
                {
                    _log.LogWarning("Stock reservation failed for sale {saleId}: {error}", saleId, error);
                    await publisher.PublishAsync("sales.rejected", new { SaleId = saleId, Status = "REJECTED", Error = error });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error procesando mensaje sales.created");
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
