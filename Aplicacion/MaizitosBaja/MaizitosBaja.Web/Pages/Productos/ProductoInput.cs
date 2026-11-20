using System.ComponentModel.DataAnnotations;
using MaizitosBaja.Entidades;

namespace MaizitosBaja.Web.Pages.Productos;

// Lo que se captura en el formulario de Crear / Editar platillo.
public class ProductoInput
{
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    [Display(Name = "Nombre del platillo")]
    public string Nombre { get; set; } = "";

    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    [Display(Name = "Descripción / ingredientes")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Escribe el precio.")]
    [Range(0.01, 99999, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Display(Name = "Precio")]
    public decimal? Precio { get; set; }

    public Producto ToEntidad() => new()
    {
        ProductoId  = ProductoId,
        Nombre      = Nombre,
        Descripcion = Descripcion,
        Precio      = Precio ?? 0
    };

    public static ProductoInput DesdeEntidad(Producto p) => new()
    {
        ProductoId  = p.ProductoId,
        Nombre      = p.Nombre,
        Descripcion = p.Descripcion,
        Precio      = p.Precio
    };
}
