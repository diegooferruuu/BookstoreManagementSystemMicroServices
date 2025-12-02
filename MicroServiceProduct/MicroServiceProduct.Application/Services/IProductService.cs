// New file: IProductService
using MicroServiceProduct.Domain.Models;
using ServiceCommon.Domain.Models;
using System;
using System.Collections.Generic;

namespace MicroServiceProduct.Application.Services
{
    public interface IProductService
    {
        void Create(Product product);
        Product? Read(Guid id);
        void Update(Product product);
        void Delete(Guid id);
        List<Product> GetAll();

        Task<PagedResult<Product>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
    }
}

