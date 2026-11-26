using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _time;

    public AfiliadosController(AppDbContext context, TimeProvider time)
    {
        _context = context;
        _time = time;
    }

    private DateTime Ahora => _time.GetLocalNow().DateTime;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var afiliados = await _context.Afiliados
            .Where(a => a.isActive)
            .ToListAsync();
        return Ok(afiliados);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null || !afiliado.isActive) return NotFound();
        return Ok(afiliado);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AfiliadoRequest request)
    {
        if (await _context.Afiliados.AnyAsync(a => a.DNI == request.DNI))
            return Conflict(new { mensaje = "Ya existe un afiliado con ese DNI." });

        var afiliado = new Afiliado
        {
            NombreCompleto = request.NombreCompleto,
            DNI = request.DNI,
            Email = request.Email,
            Telefono = request.Telefono,
            createdAt = Ahora,
            isActive = true
        };

        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = afiliado.Id }, afiliado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AfiliadoRequest request)
    {
        var existing = await _context.Afiliados.FindAsync(id);
        if (existing == null || !existing.isActive) return NotFound();

        if (await _context.Afiliados.AnyAsync(a => a.DNI == request.DNI && a.Id != id))
            return Conflict(new { mensaje = "Ya existe otro afiliado con ese DNI." });

        existing.NombreCompleto = request.NombreCompleto;
        existing.DNI = request.DNI;
        existing.Email = request.Email;
        existing.Telefono = request.Telefono;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    /// <summary>
    /// Baja lógica: el afiliado queda inactivo pero se conserva su historial de consultas.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null || !afiliado.isActive) return NotFound();

        var ahora = Ahora;
        var tieneConsultasFuturas = await _context.Consultas.AnyAsync(c =>
            c.AfiliadoId == id &&
            c.FechaHora > ahora &&
            (c.Estado == EstadoConsulta.Pendiente || c.Estado == EstadoConsulta.Confirmado));
        if (tieneConsultasFuturas)
            return BadRequest(new { mensaje = "El afiliado tiene consultas futuras activas. Cancelalas antes de darlo de baja." });

        afiliado.isActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
