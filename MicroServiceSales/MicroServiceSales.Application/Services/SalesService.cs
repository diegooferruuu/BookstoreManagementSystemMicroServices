using System;
using System.Collections.Generic;
using System.Linq;
using MicroServiceSales.Domain.Interfaces;
using MicroServiceSales.Domain.Models;
using MicroServiceSales.Domain.Validations;

namespace MicroServiceSales.Application.Services
{
    public class SalesService : ISalesService
    {
        private readonly ISalesRepository _repository;
        private readonly IEventPublisher _publisher;

        public SalesService(ISalesRepository repository, IEventPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
        }

        public List<Sale> GetAll() => _repository.GetAll();

        public Sale? Read(Guid id) => _repository.Read(id);

        public List<SaleDetail> GetDetails(Guid saleId) => _repository.GetDetails(saleId);

        public void Create(Sale sale)
        {
            var errors = SalesValidation.ValidateAll(sale).ToList();
            if (errors.Any())
                throw new ValidationException(errors);

            // Orquestación: nueva venta se crea en estado PENDING
            sale.Status = "PENDING";
            SalesValidation.Normalize(sale);
            _repository.Create(sale);

            // Persistir detalles si fueron enviados
            if (sale.Details != null && sale.Details.Any())
            {
                foreach (var d in sale.Details)
                {
                    // Asegurar subtotal coherente si no se calculó
                    if (d.Subtotal == 0)
                        d.Subtotal = d.UnitPrice * d.Quantity;
                }
                _repository.CreateDetails(sale.Id, sale.Details);
            }

            // Publicar evento a la saga de productos para disminuir stock
            var evt = new
            {
                MessageId = Guid.NewGuid().ToString(),
                SaleId = sale.Id,
                UserId = sale.UserId,
                ClientId = sale.ClientId,
                Total = sale.Total,
                SaleDate = sale.SaleDate,
                Products = (sale.Details ?? new List<SaleDetail>()).Select(d => new
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };
            // Convención: evento de creación de venta
            _publisher.PublishAsync("sales.created", evt);
        }

        public void Update(Sale sale)
        {
            var errors = SalesValidation.ValidateAll(sale).ToList();
            if (errors.Any())
                throw new ValidationException(errors);

            SalesValidation.Normalize(sale);
            _repository.Update(sale);
        }

        public void Delete(Guid id) => _repository.Delete(id);
    }
}
