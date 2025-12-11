using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroServiceClient.Domain.Models;
using MicroServiceClient.Domain.Interfaces;
using MicroServiceClient.Domain.Validations;

namespace MicroServiceClient.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientController(IClientService service)
        {
            _service = service;
        }

        // GET: api/client
        [HttpGet]
        public ActionResult<List<Client>> GetAll()
        {
            var list = _service.GetAll();
            return Ok(list);
        }

        // GET: api/client/{id}
        [HttpGet("{id:guid}")]
        public ActionResult<Client> GetById(Guid id)
        {
            var client = _service.Read(id);
            if (client is null) return NotFound();
            return Ok(client);
        }

        // POST: api/client
        [HttpPost]
        public ActionResult Create([FromBody] Client client)
        {
            try
            {
                _service.Create(client);
                // Si tu repositorio genera Id del lado BD, quizá no lo tengas aquí.
                // Puedes devolver 200 con el objeto enviado o 201 sin Location.
                return Ok(client);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "Errores de validación",
                    Errors = ex.Errors.Select(e => new { e.Field, e.Message })
                });
            }
        }

        // PUT: api/client/{id}
        [HttpPut("{id:guid}")]
        public ActionResult Update(Guid id, [FromBody] Client client)
        {
            if (client is null) return BadRequest();

            client.Id = id;

            try
            {
                _service.Update(client);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "Errores de validación",
                    Errors = ex.Errors.Select(e => new { e.Field, e.Message })
                });
            }
        }

        // DELETE: api/client/{id}
        [HttpDelete("{id:guid}")]
        public ActionResult Delete(Guid id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
