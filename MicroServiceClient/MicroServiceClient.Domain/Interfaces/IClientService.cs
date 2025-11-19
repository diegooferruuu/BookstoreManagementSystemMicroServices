using MicroServiceClient.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceClient.Domain.Interfaces
{
    public interface IClientService
    {
        List<Client> GetAll();
        Client? Read(Guid id);
        void Create(Client client);
        void Update(Client client);
        void Delete(Guid id);
    }
}
