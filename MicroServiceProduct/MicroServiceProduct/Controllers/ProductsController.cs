// New file: ProductsController
using Microsoft.AspNetCore.Mvc;
using MicroServiceProduct.Application.Services;
using MicroServiceProduct.Domain.Models;
using System;
using System.Collections.Generic;

namespace MicroServiceProduct.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _svc;

    public ProductsController(IProductService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _svc.GetAll();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public IActionResult Get(Guid id)
    {
        var p = _svc.Read(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Product model)
    {
        if (model == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest(new { message = "Name is required" });
        if (model.CategoryId == Guid.Empty) return BadRequest(new { message = "CategoryId is required" });
        model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id;
        model.CreatedAt = DateTime.UtcNow;
        _svc.Create(model);
        return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] Product model)
    {
        if (model == null) return BadRequest();
        var existing = _svc.Read(id);
        if (existing == null) return NotFound();
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.Price = model.Price;
        existing.Stock = model.Stock;
        existing.CategoryId = model.CategoryId;
        _svc.Update(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var existing = _svc.Read(id);
        if (existing == null) return NotFound();
        _svc.Delete(id);
        return NoContent();
    }
}

