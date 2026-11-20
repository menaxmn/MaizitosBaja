namespace MaizitosBaja.Entidades;

// Lo que manda la pantalla de Vender al cobrar.
// Se convierte al JSON que recibe sp_RegistrarVenta.
public class NuevaVenta
{
    public string? NombreOrden { get; set; }
    public string Tipo { get; set; } = "Venta";        // Venta o ConsumoPersonal
    public decimal Descuento { get; set; }
    public string? MotivoDescuento { get; set; }
    public decimal? EfectivoRecibido { get; set; }    // billete que dio el cliente
    public List<RenglonVenta> Items { get; set; } = new();
    public List<PagoVenta> Pagos { get; set; } = new();
}

// Un renglón: un platillo del menú o un artículo suelto
public class RenglonVenta
{
    public string Tipo { get; set; } = "";            // producto o articulo
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public string? Nota { get; set; }                 // "sin mayonesa", "sin chile" (opcional)
    public List<ArticuloElegido> Elegidos { get; set; } = new();
}

// Lo que el cliente eligió para su platillo (papitas, sopa...)
public class ArticuloElegido
{
    public int Id { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class PagoVenta
{
    public string Metodo { get; set; } = "";          // Efectivo, Tarjeta o Transferencia
    public decimal Monto { get; set; }
}
