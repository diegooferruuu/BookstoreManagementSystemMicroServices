// New file: minimal IProductRepository
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MicroServiceProduct.Domain.Models;

namespace MicroServiceProduct.Domain.Interfaces
{
    public interface IProductRepository
    {
        void Create(Product product);
        Product? Read(Guid id);
        void Update(Product product);
        void Delete(Guid id);
        List<Product> GetAll();
    }
}
