using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

/// <summary>
/// Obtiene de la base las consultas necesarias y delega la regla en <see cref="PoliticaCupoNutricion"/>.
/// </summary>
public class CupoNutricionService
{
    private readonly AppDbContext _context;

    public CupoNutricionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Devuelve el motivo de rechazo, o null si la consulta puede agendarse.</summary>
    public async Task<string?> ValidarNuevaConsultaAsync(
        int afiliadoId, Profesional profesional, ModalidadConsulta modalidad, DateTime fechaHora)
    {
        if (!PoliticaCupoNutricion.Aplica(profesional, modalidad))
            return null;

        var consultas = await ConsultasDelAnio(fechaHora.Year)
            .Where(c => c.AfiliadoId == afiliadoId)
            .ToListAsync();

        return PoliticaCupoNutricion.Validar(consultas, fechaHora);
    }

    public async Task<ResumenCupoNutricion> ObtenerResumenAsync(int afiliadoId, int anio)
    {
        var consultas = await ConsultasDelAnio(anio)
            .Where(c => c.AfiliadoId == afiliadoId)
            .ToListAsync();

        return PoliticaCupoNutricion.Resumir(afiliadoId, consultas, anio);
    }

    /// <summary>Resumen de todos los afiliados activos en una sola consulta a la base.</summary>
    public async Task<List<ResumenCupoNutricion>> ObtenerResumenesAsync(int anio)
    {
        var afiliadoIds = await _context.Afiliados
            .Where(a => a.isActive)
            .Select(a => a.Id)
            .ToListAsync();

        var consultas = await ConsultasDelAnio(anio)
            .Where(c => c.AfiliadoId != null)
            .ToListAsync();

        var porAfiliado = consultas.ToLookup(c => c.AfiliadoId!.Value);

        return afiliadoIds
            .Select(id => PoliticaCupoNutricion.Resumir(id, porAfiliado[id], anio))
            .ToList();
    }

    // El filtro fino (nutrición, presencial, no cancelada) se hace en memoria con la política,
    // para no duplicar la regla en SQL. El volumen por afiliado y año es acotado.
    private IQueryable<Consulta> ConsultasDelAnio(int anio)
    {
        var desde = new DateTime(anio, 1, 1);
        var hasta = desde.AddYears(1);
        return _context.Consultas
            .Include(c => c.Profesional)
            .Where(c => c.FechaHora >= desde && c.FechaHora < hasta);
    }
}
