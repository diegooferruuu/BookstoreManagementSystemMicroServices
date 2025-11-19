using Microsoft.AspNetCore.Mvc;
using MicroServiceUsers.Domain.Models;
using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Domain.Validations;

namespace MicroServiceUsers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        // GET: api/user
        [HttpGet]
        public ActionResult<List<User>> GetAll()
        {
            var list = _service.GetAll();
            return Ok(list);
        }

        // GET: api/user/{id}
        [HttpGet("{id:guid}")]
        public ActionResult<User> GetById(Guid id)
        {
            var user = _service.Read(id);
            if (user is null) return NotFound();
            return Ok(user);
        }

        // GET: api/user/username/{username}
        [HttpGet("username/{username}")]
        public ActionResult<User> GetByUsername(string username)
        {
            var user = _service.GetByUsername(username);
            if (user is null) return NotFound();
            return Ok(user);
        }

        // GET: api/user/email/{email}
        [HttpGet("email/{email}")]
        public ActionResult<User> GetByEmail(string email)
        {
            var user = _service.GetByEmail(email);
            if (user is null) return NotFound();
            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        public ActionResult Create([FromBody] User user)
        {
            try
            {
                _service.Create(user);
                return Ok(user);
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

        // PUT: api/user/{id}
        [HttpPut("{id:guid}")]
        public ActionResult Update(Guid id, [FromBody] User user)
        {
            if (user is null) return BadRequest();

            user.Id = id;

            try
            {
                _service.Update(user);
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

        // DELETE: api/user/{id}
        [HttpDelete("{id:guid}")]
        public ActionResult Delete(Guid id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
