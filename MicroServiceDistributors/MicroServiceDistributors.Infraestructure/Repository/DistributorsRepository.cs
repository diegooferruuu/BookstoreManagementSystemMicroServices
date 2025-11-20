using Npgsql;
using MicroServiceUsers.Domain.Interfaces;
using MicroServiceDistributors.Domain.Models;
using MicroServiceDistributors.Domain.Interfaces;

namespace ServiceDistributors.Infrastructure.Repositories
{
    public class DistributorRepository : IDistributorRepository
    {
        private readonly IDataBase _database;

        public DistributorRepository(IDataBase database)
        {
            _database = database;
        }

        public void Create(Distributor distributor)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO distributors (name, contact_email, phone, address)
                VALUES (@name, @contact_email, @phone, @address)", conn);

            cmd.Parameters.AddWithValue("@name", distributor.Name);
            cmd.Parameters.AddWithValue("@contact_email", distributor.ContactEmail ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", distributor.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", distributor.Address ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public Distributor? Read(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand("SELECT * FROM distributors WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Distributor
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ContactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString(reader.GetOrdinal("contact_email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }

        public void Update(Distributor distributor)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                UPDATE distributors SET 
                    name = @name,
                    contact_email = @contact_email,
                    phone = @phone,
                    address = @address
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", distributor.Id);
            cmd.Parameters.AddWithValue("@name", distributor.Name);
            cmd.Parameters.AddWithValue("@contact_email", distributor.ContactEmail ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", distributor.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", distributor.Address ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void Delete(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand("UPDATE distributors SET is_active = FALSE WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, id);
            cmd.ExecuteNonQuery();
        }

        public List<Distributor> GetAll()
        {
            var distributors = new List<Distributor>();
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand("SELECT * FROM distributors WHERE is_active = TRUE ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                distributors.Add(new Distributor
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ContactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString(reader.GetOrdinal("contact_email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
            return distributors;
        }
    }
}
