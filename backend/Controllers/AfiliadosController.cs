using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _time;
    private readonly CupoNutricionService _cupoNutricion;

    public AfiliadosController(AppDbContext context, TimeProvider time, CupoNutricionService cupoNutricion)
    {
        _context = context;
        _time = time;
        _cupoNutricion = cupoNutricion;
    }

    private DateTime Ahora => _time.GetLocalNow().DateTime;

    private static bool AnioValido(int? anio) => anio is null or (>= 2000 and <= 2100);

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

    /// <summary>Cupo de nutrición presencial de todos los afiliados activos para un año (por defecto, el actual).</summary>
    [HttpGet("cupo-nutricion")]
    public async Task<IActionResult> GetCuposNutricion([FromQuery] int? anio)
    {
        if (!AnioValido(anio)) return BadRequest(new { mensaje = "Año inválido." });
        var resumenes = await _cupoNutricion.ObtenerResumenesAsync(anio ?? Ahora.Year);
        return Ok(resumenes);
    }

    /// <summary>Cupo de nutrición presencial de un afiliado para un año (por defecto, el actual).</summary>
    [HttpGet("{id}/cupo-nutricion")]
    public async Task<IActionResult> GetCupoNutricion(int id, [FromQuery] int? anio)
    {
        if (!AnioValido(anio)) return BadRequest(new { mensaje = "Año inválido." });
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null || !afiliado.isActive) return NotFound();

        var resumen = await _cupoNutricion.ObtenerResumenAsync(id, anio ?? Ahora.Year);
        return Ok(resumen);
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
