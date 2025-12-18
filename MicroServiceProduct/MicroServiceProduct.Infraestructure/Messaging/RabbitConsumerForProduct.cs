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

            _exchange = cfg["RabbitMQ:Exchange"] ?? "saga.exchange";

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);

            _channel.QueueDeclare("products.queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("products.queue", _exchange, "sales.pending");
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
                Guid userId = root.GetProperty("UserId").GetGuid();
                Guid clientId = root.GetProperty("ClientId").GetGuid();
                decimal subtotal = root.GetProperty("Subtotal").GetDecimal();
                decimal total = root.GetProperty("Total").GetDecimal();
                DateTimeOffset saleDate = root.GetProperty("SaleDate").GetDateTimeOffset();
                
                var productsElem = root.GetProperty("Products");
                var items = new Dictionary<Guid, int>();
                var productsList = new List<object>();
                
                foreach (var product in productsElem.EnumerateArray())
                {
                    var pid = product.GetProperty("ProductId").GetGuid();
                    var qty = product.GetProperty("Quantity").GetInt32();
                    var price = product.GetProperty("UnitPrice").GetDecimal();
                    items[pid] = qty;
                    productsList.Add(new { ProductId = pid, Quantity = qty, UnitPrice = price });
                }

                _log.LogInformation("Procesando sales.pending: saleId={saleId} items={count}", saleId, items.Count);

                using var scope = _scopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                if (productService.TryReserveStock(items, out var error))
                {
                    _log.LogInformation("Stock reservado exitosamente para sale {saleId}", saleId);
                    // Publicar sales.approved con todos los datos de la venta
                    await publisher.PublishAsync("sales.approved", new 
                    { 
                        SaleId = saleId,
                        UserId = userId,
                        ClientId = clientId,
                        Subtotal = subtotal,
                        Total = total,
                        SaleDate = saleDate,
                        Status = "APPROVED",
                        Products = productsList
                    });
                }
                else
                {
                    _log.LogWarning("Reserva de stock fallida para sale {saleId}: {error}", saleId, error);
                    await publisher.PublishAsync("sales.approved", new 
                    { 
                        SaleId = saleId,
                        UserId = userId,
                        ClientId = clientId,
                        Subtotal = subtotal,
                        Total = total,
                        SaleDate = saleDate,
                        Status = "REJECTED", 
                        Error = error 
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error procesando mensaje sales.pending");
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
