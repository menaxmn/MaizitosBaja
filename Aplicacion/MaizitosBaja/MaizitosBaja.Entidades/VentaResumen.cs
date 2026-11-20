namespace MaizitosBaja.Entidades;

// Una venta ya registrada, para listarla en la pantalla de Caja.
public class VentaResumen
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public string? NombreOrden { get; set; }
    public string Tipo { get; set; } = "";
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public decimal? Cambio { get; set; }
    public string Estado { get; set; } = "";
    public string Conceptos { get; set; } = "";   // "1 Tostilocos, 2 Coca Zero"
    public string Pagos { get; set; } = "";       // "Tarjeta $100.00 + Efectivo $75.00"
}
