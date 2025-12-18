// New file: IProductService
using System;
using System.Collections.Generic;
using MicroServiceProduct.Domain.Models;

namespace MicroServiceProduct.Application.Services
{
    public interface IProductService
    {
        void Create(Product product);
        Product? Read(Guid id);
        void Update(Product product);
        void Delete(Guid id);
        List<Product> GetAll();
        bool TryReserveStock(Dictionary<Guid, int> items, out string? error);
    }
}

