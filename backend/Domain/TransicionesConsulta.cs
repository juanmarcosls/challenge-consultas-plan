using ConsultasPlan.Models;

namespace ConsultasPlan.Domain;

/// <summary>
/// Reglas de cambio de estado de una consulta. Centraliza las validaciones para que
/// ningún endpoint (incluido el de actualización genérica de estado) pueda saltearlas.
/// </summary>
public static class TransicionesConsulta
{
    public static readonly TimeSpan AnticipacionMinimaCancelacion = TimeSpan.FromHours(24);

    private static readonly Dictionary<EstadoConsulta, EstadoConsulta[]> Permitidas = new()
    {
        [EstadoConsulta.Pendiente] = new[]
        {
            EstadoConsulta.Confirmado, EstadoConsulta.Cancelado,
            EstadoConsulta.Atendido, EstadoConsulta.Inasistencia
        },
        [EstadoConsulta.Confirmado] = new[]
        {
            EstadoConsulta.Cancelado, EstadoConsulta.Atendido, EstadoConsulta.Inasistencia
        },
        // Cancelado, Atendido e Inasistencia son estados finales.
        [EstadoConsulta.Cancelado] = Array.Empty<EstadoConsulta>(),
        [EstadoConsulta.Atendido] = Array.Empty<EstadoConsulta>(),
        [EstadoConsulta.Inasistencia] = Array.Empty<EstadoConsulta>()
    };

    /// <summary>
    /// Devuelve el motivo por el cual la transición no es válida, o null si se puede realizar.
    /// </summary>
    public static string? Validar(Consulta consulta, EstadoConsulta nuevoEstado, DateTime ahora)
    {
        if (consulta.Estado == nuevoEstado)
            return $"La consulta ya se encuentra en estado {nuevoEstado}.";

        if (!Permitidas.TryGetValue(consulta.Estado, out var destinos) || !destinos.Contains(nuevoEstado))
            return $"No se puede pasar una consulta en estado {consulta.Estado} a {nuevoEstado}.";

        switch (nuevoEstado)
        {
            case EstadoConsulta.Cancelado when consulta.FechaHora - ahora < AnticipacionMinimaCancelacion:
                return "No se puede cancelar con menos de 24 horas de anticipación.";

            case EstadoConsulta.Atendido when consulta.FechaHora > ahora:
                return "No se puede registrar la atención de una consulta futura.";

            case EstadoConsulta.Inasistencia when consulta.FechaHora > ahora:
                return "No se puede registrar una inasistencia sobre una consulta futura.";
        }

        return null;
    }
}
