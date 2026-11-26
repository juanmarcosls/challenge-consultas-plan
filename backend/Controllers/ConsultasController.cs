using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Domain;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _time;
    private readonly CupoNutricionService _cupoNutricion;

    public ConsultasController(AppDbContext context, TimeProvider time, CupoNutricionService cupoNutricion)
    {
        _context = context;
        _time = time;
        _cupoNutricion = cupoNutricion;
    }

    private DateTime Ahora => _time.GetLocalNow().DateTime;

    private IQueryable<Consulta> ConsultasConDetalle() => _context.Consultas
        .Include(c => c.Afiliado)
        .Include(c => c.Profesional);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await ConsultasConDetalle()
            .OrderBy(c => c.FechaHora)
            .ToListAsync();
        return Ok(consultas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await ConsultasConDetalle().FirstOrDefaultAsync(c => c.Id == id);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request)
    {
        var fechaHora = request.FechaHora!.Value;

        var afiliado = await _context.Afiliados.FindAsync(request.AfiliadoId);
        if (afiliado == null || !afiliado.isActive)
            return NotFound(new { mensaje = "Afiliado no encontrado." });

        if (afiliado.Bloqueado)
            return BadRequest(new { mensaje = "El afiliado se encuentra bloqueado para agendar consultas." });

        var profesional = await _context.Profesionales.FindAsync(request.ProfesionalId);
        if (profesional == null)
            return NotFound(new { mensaje = "Profesional no encontrado." });

        if (fechaHora <= Ahora)
            return BadRequest(new { mensaje = "La consulta debe agendarse en una fecha y hora futura." });

        var conflictoProfesional = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == request.ProfesionalId &&
            c.FechaHora == fechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (conflictoProfesional)
            return BadRequest(new { mensaje = "El profesional ya tiene una consulta en ese horario." });

        var conflictoAfiliado = await _context.Consultas.AnyAsync(c =>
            c.AfiliadoId == request.AfiliadoId &&
            c.FechaHora == fechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (conflictoAfiliado)
            return BadRequest(new { mensaje = "El afiliado ya tiene otra consulta en ese horario." });

        var modalidad = request.Modalidad!.Value;
        var errorCupo = await _cupoNutricion.ValidarNuevaConsultaAsync(afiliado.Id, profesional, modalidad, fechaHora);
        if (errorCupo != null)
            return BadRequest(new { mensaje = errorCupo });

        var consulta = new Consulta
        {
            AfiliadoId = request.AfiliadoId,
            ProfesionalId = request.ProfesionalId,
            FechaHora = fechaHora,
            Modalidad = modalidad,
            Motivo = request.Motivo,
            FechaCreacion = Ahora,
            Estado = EstadoConsulta.Pendiente
        };

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();

        var creada = await ConsultasConDetalle().FirstAsync(c => c.Id == consulta.Id);
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, creada);
    }

    [HttpPost("{id}/confirmar")]
    public Task<IActionResult> Confirmar(int id) => CambiarEstado(id, EstadoConsulta.Confirmado);

    [HttpPost("{id}/cancelar")]
    public Task<IActionResult> CancelarConsulta(int id) => CambiarEstado(id, EstadoConsulta.Cancelado);

    [HttpPost("{id}/inasistencia")]
    public Task<IActionResult> MarcarInasistencia(int id) => CambiarEstado(id, EstadoConsulta.Inasistencia);

    [HttpPost("{id}/atencion")]
    public Task<IActionResult> RegistrarAtencion(int id) => CambiarEstado(id, EstadoConsulta.Atendido);

    /// <summary>
    /// Cambio de estado genérico. Aplica las mismas reglas que los endpoints específicos.
    /// </summary>
    [HttpPut("{id}/estado")]
    public Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
        => CambiarEstado(id, request.Estado);

    private async Task<IActionResult> CambiarEstado(int id, EstadoConsulta nuevoEstado)
    {
        var consulta = await ConsultasConDetalle().FirstOrDefaultAsync(c => c.Id == id);
        if (consulta == null) return NotFound();

        var error = TransicionesConsulta.Validar(consulta, nuevoEstado, Ahora);
        if (error != null)
            return BadRequest(new { mensaje = error });

        consulta.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}
