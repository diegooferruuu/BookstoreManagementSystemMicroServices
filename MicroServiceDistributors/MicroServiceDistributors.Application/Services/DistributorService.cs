using MicroServiceDistributors.Domain.Interfaces;
using MicroServiceDistributors.Domain.Models;
using MicroServiceDistributors.Domain.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceDistributors.Application.Services
{
    public class DistributorService : IDistributorService
    {
        private readonly IDistributorRepository _repository;

        public DistributorService(IDistributorRepository repository)
        {
            _repository = repository;
        }

        public List<Distributor> GetAll() => _repository.GetAll();
        public Distributor? Read(Guid id) => _repository.Read(id);

        public void Create(Distributor distributor)
        {
            var errors = DistributorValidation.Validate(distributor);
            if (errors != null && errors.Any())
                throw new ValidationException(errors);

            DistributorValidation.Normalize(distributor);
            _repository.Create(distributor);
        }

        public void Update(Distributor distributor)
        {
            var errors = DistributorValidation.Validate(distributor);
            if (errors != null && errors.Any())
                throw new ValidationException(errors);

            DistributorValidation.Normalize(distributor);
            _repository.Update(distributor);
        }

        public void Delete(Guid id) => _repository.Delete(id);
    }
}
