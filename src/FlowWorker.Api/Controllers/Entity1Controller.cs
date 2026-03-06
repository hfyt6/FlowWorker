using FlowWorker.Infrastructure;
using FlowWorker.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowWorker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Entity1Controller : ControllerBase
{
    private readonly AppDbContext _context;

    public Entity1Controller(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/entity1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Entity1>>> GetEntity1s()
    {
        return await _context.Entity1s.ToListAsync();
    }

    // GET: api/entity1/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Entity1>> GetEntity1(Guid id)
    {
        var entity = await _context.Entity1s.FindAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        return entity;
    }

    // POST: api/entity1
    [HttpPost]
    public async Task<ActionResult<Entity1>> PostEntity1(Entity1 entity)
    {
        entity.Id = Guid.NewGuid();
        _context.Entity1s.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEntity1), new { id = entity.Id }, entity);
    }

    // PUT: api/entity1/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEntity1(Guid id, Entity1 entity)
    {
        if (id != entity.Id)
        {
            return BadRequest();
        }

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/entity1/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntity1(Guid id)
    {
        var entity = await _context.Entity1s.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Entity1s.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}