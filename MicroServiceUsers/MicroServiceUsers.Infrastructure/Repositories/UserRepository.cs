using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceUsers.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDataBase _database;

        public UserRepository(IDataBase database)
        {
            _database = database;
        }

        public List<User> GetAll()
        {
            var users = new List<User>();
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, username, email, first_name, last_name, middle_name, 
                       password_hash, is_active, must_change_password, created_at 
                FROM users 
                WHERE is_active = TRUE 
                ORDER BY last_name, first_name", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    MustChangePassword = reader.GetBoolean(reader.GetOrdinal("must_change_password")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return users;
        }

        public User? Read(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, username, email, first_name, last_name, middle_name, 
                       password_hash, is_active, must_change_password, created_at 
                FROM users 
                WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    MustChangePassword = reader.GetBoolean(reader.GetOrdinal("must_change_password")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }

        public void Create(User user)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO users (username, email, first_name, last_name, middle_name, 
                                   password_hash, is_active, must_change_password)
                VALUES (@username, @email, @first_name, @last_name, @middle_name, 
                        @password_hash, @is_active, @must_change_password)", conn);

            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@first_name", user.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_name", user.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@middle_name", user.MiddleName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@is_active", user.IsActive);
            cmd.Parameters.AddWithValue("@must_change_password", user.MustChangePassword);

            cmd.ExecuteNonQuery();
        }

        public void Update(User user)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                UPDATE users SET 
                    username = @username,
                    email = @email,
                    first_name = @first_name,
                    last_name = @last_name,
                    middle_name = @middle_name,
                    password_hash = @password_hash,
                    is_active = @is_active,
                    must_change_password = @must_change_password
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@first_name", user.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_name", user.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@middle_name", user.MiddleName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@is_active", user.IsActive);
            cmd.Parameters.AddWithValue("@must_change_password", user.MustChangePassword);

            cmd.ExecuteNonQuery();
        }

        public void Delete(Guid id)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand("UPDATE users SET is_active = FALSE WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, id);
            cmd.ExecuteNonQuery();
        }

        public User? GetByUsername(string username)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, username, email, first_name, last_name, middle_name, 
                       password_hash, is_active, must_change_password, created_at 
                FROM users 
                WHERE LOWER(username) = LOWER(@username)", conn);
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    MustChangePassword = reader.GetBoolean(reader.GetOrdinal("must_change_password")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }

        public User? GetByEmail(string email)
        {
            using var conn = _database.GetConnection();
            using var cmd = new NpgsqlCommand(@"
                SELECT id, username, email, first_name, last_name, middle_name, 
                       password_hash, is_active, must_change_password, created_at 
                FROM users 
                WHERE LOWER(email) = LOWER(@email)", conn);
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    MustChangePassword = reader.GetBoolean(reader.GetOrdinal("must_change_password")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }

            return null;
        }
    }
}
