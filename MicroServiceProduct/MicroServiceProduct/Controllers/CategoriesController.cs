// New file: CategoriesController
using Microsoft.AspNetCore.Mvc;
using MicroServiceProduct.Domain.Interfaces;
using MicroServiceProduct.Domain.Models;
using System;
using System.Collections.Generic;

namespace MicroServiceProduct.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;

    public CategoriesController(ICategoryRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _repo.GetAll();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public IActionResult Get(Guid id)
    {
        var c = _repo.Read(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Category model)
    {
        if (model == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest(new { message = "Name is required" });
        model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id;
        model.CreatedAt = DateTime.UtcNow;
        _repo.Create(model);
        return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] Category model)
    {
        if (model == null) return BadRequest();
        var existing = _repo.Read(id);
        if (existing == null) return NotFound();
        existing.Name = model.Name;
        existing.Description = model.Description;
        _repo.Update(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var existing = _repo.Read(id);
        if (existing == null) return NotFound();
        _repo.Delete(id);
        return NoContent();
    }
}

