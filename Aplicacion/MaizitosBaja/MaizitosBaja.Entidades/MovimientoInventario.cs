namespace MaizitosBaja.Entidades;

// Un renglón del historial de inventario (tabla MovimientoInventario).
// Cantidad positiva = entró, negativa = salió.
public class MovimientoInventario
{
    public int MovimientoId { get; set; }
    public int ArticuloId { get; set; }
    public string Articulo { get; set; } = "";       // nombre completo, para mostrar
    public string Categoria { get; set; } = "";
    public string Tipo { get; set; } = "";           // Inicial, Entrada, Venta, ConsumoPersonal, Uso, Merma, Ajuste, Cancelacion
    public int Cantidad { get; set; }
    public int? StockAnterior { get; set; }          // lo llena el trigger
    public int? StockNuevo { get; set; }
    public DateTime Fecha { get; set; }
    public int? ProveedorId { get; set; }
    public string? Proveedor { get; set; }
    public int? VentaId { get; set; }
    public string? Observaciones { get; set; }
}

// Un artículo dentro de una entrada (compra) de varios artículos
public class RenglonEntrada
{
    public int ArticuloId { get; set; }
    public int Cantidad { get; set; }
}
