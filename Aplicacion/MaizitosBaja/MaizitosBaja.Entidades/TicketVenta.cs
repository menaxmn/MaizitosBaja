namespace MaizitosBaja.Entidades;

// Todo lo que se imprime en el ticket de una venta.
public class TicketVenta
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public string? NombreOrden { get; set; }
    public string Tipo { get; set; } = "";          // Venta o ConsumoPersonal
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public string? MotivoDescuento { get; set; }
    public decimal Total { get; set; }
    public decimal? EfectivoRecibido { get; set; }
    public decimal? Cambio { get; set; }
    public string Estado { get; set; } = "";        // Pagada o Cancelada
    public List<TicketRenglon> Renglones { get; set; } = new();
    public List<PagoVenta> Pagos { get; set; } = new();
}

public class TicketRenglon
{
    public int Cantidad { get; set; }
    public string Concepto { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public string? Nota { get; set; }
    public string? Elegidos { get; set; }           // "Tostitos Verdes, Doritos Rojos"
}
