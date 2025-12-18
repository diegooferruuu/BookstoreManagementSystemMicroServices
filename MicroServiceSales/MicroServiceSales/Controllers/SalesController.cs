using Microsoft.AspNetCore.Mvc;
using MicroServiceSales.Domain.Interfaces;
using MicroServiceSales.Domain.Models;
using MicroServiceSales.Domain.Validations;

namespace MicroServiceSales.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _service;

        public SalesController(ISalesService service)
        {
            _service = service;
        }

        // GET: api/sales
        [HttpGet]
        public ActionResult<List<Sale>> GetAll()
        {
            var list = _service.GetAll();
            return Ok(list);
        }

        // GET: api/sales/{id}
        [HttpGet("{id:guid}")]
        public ActionResult<Sale> GetById(Guid id)
        {
            var sale = _service.Read(id);
            if (sale is null) return NotFound();
            // eager-load details for convenience
            sale.Details = _service.GetDetails(id);
            return Ok(sale);
        }

        // GET: api/sales/{id}/details
        [HttpGet("{id:guid}/details")]
        public ActionResult<List<SaleDetail>> GetDetails(Guid id)
        {
            var sale = _service.Read(id);
            if (sale is null) return NotFound();
            var details = _service.GetDetails(id);
            return Ok(details);
        }

        // POST: api/sales
        [HttpPost]
        public ActionResult Create([FromBody] Sale sale)
        {
            try
            {
                _service.Create(sale);
                return Ok(sale);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "Errores de validaci�n",
                    Errors = ex.Errors.Select(e => new { e.Field, e.Message })
                });
            }
        }

        // PUT: api/sales/{id}
        [HttpPut("{id:guid}")]
        public ActionResult Update(Guid id, [FromBody] Sale sale)
        {
            if (sale is null) return BadRequest();
            sale.Id = id;
            try
            {
                _service.Update(sale);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    Message = "Errores de validaci�n",
                    Errors = ex.Errors.Select(e => new { e.Field, e.Message })
                });
            }
        }

        // DELETE: api/sales/{id}
        [HttpDelete("{id:guid}")]
        public ActionResult Delete(Guid id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
