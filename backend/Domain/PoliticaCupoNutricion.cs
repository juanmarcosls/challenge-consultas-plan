using System.Globalization;
using System.Text;
using ConsultasPlan.Models;

namespace ConsultasPlan.Domain;

/// <summary>
/// Política de cupo: el plan incluye 6 consultas de nutrición por año, no más de 1 por mes.
/// Las consultas no usadas no se acumulan. Aplica a consultas presenciales.
///
/// Supuestos (ver documento técnico):
/// - Año y mes calendario, tomados de la fecha de la consulta (no de la fecha en que se reserva).
/// - Consumen cupo las consultas Pendiente, Confirmado, Atendido e Inasistencia. Cancelado no.
/// - Las consultas virtuales no consumen cupo ni están limitadas.
/// </summary>
public static class PoliticaCupoNutricion
{
    public const int CupoAnual = 6;
    public const int MaximoPorMes = 1;
    public const string EspecialidadNutricion = "Nutrición";

    /// <summary>Compara sin distinguir mayúsculas ni tildes ("Nutrición", "nutricion", "NUTRICION").</summary>
    public static bool EsNutricion(string? especialidad) =>
        especialidad != null && Normalizar(especialidad) == Normalizar(EspecialidadNutricion);

    /// <summary>Indica si una consulta con este profesional y modalidad está alcanzada por la política.</summary>
    public static bool Aplica(Profesional profesional, ModalidadConsulta modalidad) =>
        modalidad == ModalidadConsulta.Presencial && EsNutricion(profesional.Especialidad);

    /// <summary>Indica si una consulta existente cuenta para el cupo. Requiere Profesional cargado.</summary>
    public static bool ConsumeCupo(Consulta consulta) =>
        consulta.Estado != EstadoConsulta.Cancelado &&
        consulta.Profesional != null &&
        Aplica(consulta.Profesional, consulta.Modalidad);

    /// <summary>
    /// Valida si el afiliado puede agendar una nueva consulta alcanzada por la política en la fecha indicada.
    /// Devuelve el motivo del rechazo o null si está dentro del cupo.
    /// </summary>
    public static string? Validar(IEnumerable<Consulta> consultasDelAfiliado, DateTime fechaNueva)
    {
        var delAnio = consultasDelAfiliado
            .Where(c => ConsumeCupo(c) && c.FechaHora.Year == fechaNueva.Year)
            .ToList();

        if (delAnio.Count >= CupoAnual)
            return $"El afiliado ya utilizó las {CupoAnual} consultas de nutrición presencial del año {fechaNueva.Year}.";

        var delMes = delAnio.Count(c => c.FechaHora.Month == fechaNueva.Month);
        if (delMes >= MaximoPorMes)
        {
            var mes = fechaNueva.ToString("MMMM 'de' yyyy", new CultureInfo("es-AR"));
            return $"El afiliado ya tiene una consulta de nutrición presencial en {mes} (máximo {MaximoPorMes} por mes).";
        }

        return null;
    }

    public static ResumenCupoNutricion Resumir(int afiliadoId, IEnumerable<Consulta> consultasDelAfiliado, int anio)
    {
        var delAnio = consultasDelAfiliado
            .Where(c => ConsumeCupo(c) && c.FechaHora.Year == anio)
            .ToList();

        return new ResumenCupoNutricion(
            AfiliadoId: afiliadoId,
            Anio: anio,
            CupoAnual: CupoAnual,
            Usadas: delAnio.Count,
            Disponibles: Math.Max(0, CupoAnual - delAnio.Count),
            MesesUtilizados: delAnio.Select(c => c.FechaHora.Month).Distinct().Order().ToArray());
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);
        foreach (var ch in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sinTildes.Append(ch);
        return sinTildes.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}

public record ResumenCupoNutricion(
    int AfiliadoId,
    int Anio,
    int CupoAnual,
    int Usadas,
    int Disponibles,
    int[] MesesUtilizados);
