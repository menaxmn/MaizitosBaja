namespace MaizitosBaja.Entidades;

// Todo lo que hay en el inventario: comida, bebida y material.
// Corresponde a la tabla Articulo.
public class Articulo
{
    public int ArticuloId { get; set; }
    public int CategoriaId { get; set; }
    public int? ProveedorId { get; set; }
    public string Nombre { get; set; } = "";
    public string Variante { get; set; } = "";      // '' si no tiene
    public string Unidad { get; set; } = "Pieza";   // Pieza o Paquete
    public int Stock { get; set; }                  // solo lo cambian los movimientos
    public int StockMinimo { get; set; }
    public bool SeVendeSuelto { get; set; }
    public decimal? PrecioSuelto { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; }

    // Datos de otras tablas, solo para mostrar (vienen del JOIN)
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string? Proveedor { get; set; }

    // "Coca Zero", o solo "Coca" si no tiene variante
    public string NombreCompleto =>
        string.IsNullOrWhiteSpace(Variante) ? Nombre : $"{Nombre} {Variante}";

    // Mismo criterio que la vista vw_InventarioAlertas
    public string EstadoStock =>
        Stock == 0 ? "Agotado" :
        Stock <= StockMinimo ? "Stock bajo" : "OK";
}
