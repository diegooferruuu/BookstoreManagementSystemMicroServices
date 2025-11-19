using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Domain.Models;
using MicroServiceUsers.Domain.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceUsers.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public List<User> GetAll() => _repository.GetAll();

        public User? Read(Guid id) => _repository.Read(id);

        public void Create(User user)
        {
            var errors = UserValidation.Validate(user).ToList();
            if (errors.Any())
                throw new ValidationException(errors);

            UserValidation.Normalize(user);
            _repository.Create(user);
        }

        public void Update(User user)
        {
            var errors = UserValidation.Validate(user).ToList();
            if (errors.Any())
                throw new ValidationException(errors);

            UserValidation.Normalize(user);
            _repository.Update(user);
        }

        public void Delete(Guid id) => _repository.Delete(id);

        public User? GetByUsername(string username) => _repository.GetByUsername(username);

        public User? GetByEmail(string email) => _repository.GetByEmail(email);
    }
}
