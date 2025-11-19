using MicroServiceUsers.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceUsers.Domain.Interfaces
{
    public interface IUserRepository
    {
        List<User> GetAll();
        User? Read(Guid id);
        void Create(User user);
        void Update(User user);
        void Delete(Guid id);
        User? GetByUsername(string username);
        User? GetByEmail(string email);
    }
}
