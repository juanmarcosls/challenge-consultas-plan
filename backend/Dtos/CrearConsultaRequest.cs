using System.ComponentModel.DataAnnotations;
using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos para agendar una consulta. Estado, FechaCreacion e Id los define el servidor.
/// </summary>
public class CrearConsultaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un afiliado.")]
    public int AfiliadoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un profesional.")]
    public int ProfesionalId { get; set; }

    [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
    public DateTime? FechaHora { get; set; }

    [Required(ErrorMessage = "La modalidad es obligatoria.")]
    public ModalidadConsulta? Modalidad { get; set; }

    [StringLength(200)]
    public string Motivo { get; set; } = string.Empty;
}
