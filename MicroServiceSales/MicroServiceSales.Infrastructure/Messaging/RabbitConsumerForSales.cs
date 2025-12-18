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
            _exchange = cfg["RabbitMQ:Exchange"] ?? "saga.exchange";

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);

            // Cola enfocada a procesos de ventas
            _channel.QueueDeclare("sales.queue", durable: true, exclusive: false, autoDelete: false);
            // Escuchamos la aprobación de stock desde el microservicio de productos
            _channel.QueueBind("sales.queue", _exchange, "sales.approved");
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
                string status = root.GetProperty("Status").GetString() ?? "UNKNOWN";

                _log.LogInformation("Procesando sales.approved: saleId={saleId} status={status}", saleId, status);

                using var scope = _scopeFactory.CreateScope();
                var salesRepo = scope.ServiceProvider.GetRequiredService<ISalesRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                try
                {
                    if (status == "APPROVED")
                    {
                        // Reconstruir la venta desde el mensaje
                        var sale = new MicroServiceSales.Domain.Models.Sale
                        {
                            Id = saleId,
                            UserId = root.GetProperty("UserId").GetGuid(),
                            ClientId = root.GetProperty("ClientId").GetGuid(),
                            Subtotal = root.GetProperty("Subtotal").GetDecimal(),
                            Total = root.GetProperty("Total").GetDecimal(),
                            SaleDate = root.GetProperty("SaleDate").GetDateTimeOffset(),
                            Status = "COMPLETED",
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        // Reconstruir detalles
                        var details = new List<MicroServiceSales.Domain.Models.SaleDetail>();
                        var productsElem = root.GetProperty("Products");
                        foreach (var prod in productsElem.EnumerateArray())
                        {
                            var detail = new MicroServiceSales.Domain.Models.SaleDetail
                            {
                                Id = Guid.NewGuid(),
                                SaleId = saleId,
                                ProductId = prod.GetProperty("ProductId").GetGuid(),
                                Quantity = prod.GetProperty("Quantity").GetInt32(),
                                UnitPrice = prod.GetProperty("UnitPrice").GetDecimal(),
                                Subtotal = prod.GetProperty("UnitPrice").GetDecimal() * prod.GetProperty("Quantity").GetInt32()
                            };
                            details.Add(detail);
                        }

                        // Guardar en DB
                        salesRepo.Create(sale);
                        salesRepo.CreateDetails(saleId, details);

                        _log.LogInformation("Venta {saleId} guardada en DB con estado COMPLETED", saleId);

                        // Publicar sales.confirmed para que otros microservicios puedan procesarla
                        await publisher.PublishAsync("sales.confirmed", new
                        {
                            SaleId = saleId,
                            UserId = sale.UserId,
                            ClientId = sale.ClientId,
                            Total = sale.Total,
                            Status = "CONFIRMED",
                            SaleDate = sale.SaleDate,
                            Products = details.Select(d => new
                            {
                                ProductId = d.ProductId,
                                Quantity = d.Quantity,
                                UnitPrice = d.UnitPrice,
                                Subtotal = d.Subtotal
                            }).ToList()
                        });

                        _log.LogInformation("Evento sales.confirmed publicado para saleId={saleId}", saleId);
                    }
                    else if (status == "REJECTED")
                    {
                        string error = root.GetProperty("Error").GetString() ?? "Unknown error";
                        _log.LogWarning("Venta {saleId} rechazada: {error}", saleId, error);
                        // Aquí podrías notificar al cliente o tomar otra acción
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error procesando aprobación de venta");
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error procesando mensaje sales.approved");
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
