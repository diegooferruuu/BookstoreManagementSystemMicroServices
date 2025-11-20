using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MicroServiceProduct.Domain.Models;
using MicroServiceProduct.Domain.Interfaces;
using MicroServiceProduct.Domain.Services;

namespace MicroServiceProduct.Infraestructure.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDataBase _database;

        public ProductRepository(IDataBase database)
        {
            _database = database;
        }

        public void Create(Product product)
        {
            using var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO products (name, description, category_id, price, stock) VALUES (@name, @description, @category_id, @price, @stock)";

            var pName = cmd.CreateParameter(); pName.ParameterName = "@name"; pName.Value = product.Name; cmd.Parameters.Add(pName);
            var pDesc = cmd.CreateParameter(); pDesc.ParameterName = "@description"; pDesc.Value = (object?)product.Description ?? DBNull.Value; cmd.Parameters.Add(pDesc);
            var pCat = cmd.CreateParameter(); pCat.ParameterName = "@category_id"; pCat.Value = product.CategoryId != Guid.Empty ? (object)product.CategoryId : DBNull.Value; cmd.Parameters.Add(pCat);
            var pPrice = cmd.CreateParameter(); pPrice.ParameterName = "@price"; pPrice.Value = product.Price; cmd.Parameters.Add(pPrice);
            var pStock = cmd.CreateParameter(); pStock.ParameterName = "@stock"; pStock.Value = product.Stock; cmd.Parameters.Add(pStock);

            cmd.ExecuteNonQuery();
        }

        public Product? Read(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT p.*, c.name AS category_name FROM products p LEFT JOIN categories c ON p.category_id = c.id WHERE p.id = @id";
            var pid = cmd.CreateParameter(); pid.ParameterName = "@id"; pid.Value = id; cmd.Parameters.Add(pid);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Product
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    CategoryId = reader.IsDBNull(reader.GetOrdinal("category_id")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("category_id")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("category_name")) ? null : reader.GetString(reader.GetOrdinal("category_name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                    Stock = reader.GetInt32(reader.GetOrdinal("stock")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }

        public void Update(Product product)
        {
            using var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE products SET name = @name, description = @description, category_id = @category_id, price = @price, stock = @stock WHERE id = @id";

            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = product.Id; cmd.Parameters.Add(pId);
            var pName = cmd.CreateParameter(); pName.ParameterName = "@name"; pName.Value = product.Name; cmd.Parameters.Add(pName);
            var pDesc = cmd.CreateParameter(); pDesc.ParameterName = "@description"; pDesc.Value = (object?)product.Description ?? DBNull.Value; cmd.Parameters.Add(pDesc);
            var pCat = cmd.CreateParameter(); pCat.ParameterName = "@category_id"; pCat.Value = product.CategoryId != Guid.Empty ? (object)product.CategoryId : DBNull.Value; cmd.Parameters.Add(pCat);
            var pPrice = cmd.CreateParameter(); pPrice.ParameterName = "@price"; pPrice.Value = product.Price; cmd.Parameters.Add(pPrice);
            var pStock = cmd.CreateParameter(); pStock.ParameterName = "@stock"; pStock.Value = product.Stock; cmd.Parameters.Add(pStock);

            cmd.ExecuteNonQuery();
        }

        public void Delete(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE products SET is_active = FALSE WHERE id = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            cmd.ExecuteNonQuery();
        }

        public List<Product> GetAll()
        {
            var products = new List<Product>();
            using var conn = _database.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT p.*, c.name AS category_name FROM products p LEFT JOIN categories c ON p.category_id = c.id WHERE p.is_active = TRUE ORDER BY p.name";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    CategoryId = reader.IsDBNull(reader.GetOrdinal("category_id")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("category_id")),
                    CategoryName = reader.IsDBNull(reader.GetOrdinal("category_name")) ? null : reader.GetString(reader.GetOrdinal("category_name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                    Stock = reader.GetInt32(reader.GetOrdinal("stock")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
            return products;
        }
    }
}
