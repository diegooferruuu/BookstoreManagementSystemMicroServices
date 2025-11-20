using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroservicioCliente.Infrastucture.Persistence
{
    public class MySqlConnectionDB
    {
        private readonly string connectionString;

        public MySqlConnectionDB(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("MysqlMicroServiceDistributorsoDB");
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }



    }
}
