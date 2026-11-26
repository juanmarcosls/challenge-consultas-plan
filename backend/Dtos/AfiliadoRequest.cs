using System.ComponentModel.DataAnnotations;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos editables de un afiliado. Campos como Bloqueado, ConsultasUsadas o isActive
/// no se exponen para evitar que el cliente los modifique (overposting).
/// </summary>
public class AfiliadoRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos, sin puntos.")]
    public string DNI { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;
}
