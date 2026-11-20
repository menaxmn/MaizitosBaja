using System.ComponentModel.DataAnnotations;
using MaizitosBaja.Entidades;

namespace MaizitosBaja.Web.Pages.Inventario;

// Lo que se captura en el formulario de Crear / Editar.
// Las validaciones de aquí son las rápidas (en pantalla);
// las reglas completas están en ArticuloNegocio.
public class ArticuloInput
{
    public int ArticuloId { get; set; }

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int? CategoriaId { get; set; }

    [Display(Name = "Proveedor")]
    public int? ProveedorId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = "";

    [StringLength(40, ErrorMessage = "Máximo 40 caracteres.")]
    [Display(Name = "Variante (sabor, tipo, tamaño)")]
    public string? Variante { get; set; }

    [Display(Name = "Unidad")]
    public string Unidad { get; set; } = "Pieza";

    [Range(0, 100000, ErrorMessage = "Debe ser 0 o más.")]
    [Display(Name = "Existencia inicial")]
    public int StockInicial { get; set; }

    [Range(0, 100000, ErrorMessage = "Debe ser 0 o más.")]
    [Display(Name = "Stock mínimo (alerta)")]
    public int StockMinimo { get; set; }

    [Display(Name = "Se vende suelto")]
    public bool SeVendeSuelto { get; set; }

    [Range(0.01, 99999, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Display(Name = "Precio suelto")]
    public decimal? PrecioSuelto { get; set; }

    // Solo para mostrar en Editar
    public int StockActual { get; set; }

    public Articulo ToEntidad() => new()
    {
        ArticuloId    = ArticuloId,
        CategoriaId   = CategoriaId ?? 0,
        ProveedorId   = ProveedorId,
        Nombre        = Nombre,
        Variante      = Variante ?? "",
        Unidad        = Unidad,
        StockMinimo   = StockMinimo,
        SeVendeSuelto = SeVendeSuelto,
        PrecioSuelto  = PrecioSuelto
    };

    public static ArticuloInput DesdeEntidad(Articulo a) => new()
    {
        ArticuloId    = a.ArticuloId,
        CategoriaId   = a.CategoriaId,
        ProveedorId   = a.ProveedorId,
        Nombre        = a.Nombre,
        Variante      = a.Variante,
        Unidad        = a.Unidad,
        StockMinimo   = a.StockMinimo,
        SeVendeSuelto = a.SeVendeSuelto,
        PrecioSuelto  = a.PrecioSuelto,
        StockActual   = a.Stock
    };
}
