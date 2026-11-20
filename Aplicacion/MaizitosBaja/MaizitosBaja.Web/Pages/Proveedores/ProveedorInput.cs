using System.ComponentModel.DataAnnotations;
using MaizitosBaja.Entidades;

namespace MaizitosBaja.Web.Pages.Proveedores;

public class ProveedorInput
{
    public int ProveedorId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Nombre o negocio")]
    public string Nombre { get; set; } = "";

    [RegularExpression(@"^[0-9 +()\-]{7,15}$", ErrorMessage = "Solo números, espacios o guiones (7 a 15).")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Revisa el correo.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Correo")]
    public string? Correo { get; set; }

    public Proveedor ToEntidad() => new()
    {
        ProveedorId = ProveedorId,
        Nombre      = Nombre,
        Telefono    = Telefono,
        Correo      = Correo
    };

    public static ProveedorInput DesdeEntidad(Proveedor p) => new()
    {
        ProveedorId = p.ProveedorId,
        Nombre      = p.Nombre,
        Telefono    = p.Telefono,
        Correo      = p.Correo
    };
}
