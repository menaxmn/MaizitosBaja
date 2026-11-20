using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Reglas del punto de venta. Las validaciones de dinero e inventario
// las vuelve a revisar sp_RegistrarVenta (y los triggers), aquí se
// revisa lo básico para dar mensajes claros.
public class VentaNegocio
{
    private static readonly string[] Metodos = { "Efectivo", "Tarjeta", "Transferencia" };

    private readonly VentaDatos _ventas;
    private readonly ProductoDatos _productos;
    private readonly ArticuloDatos _articulos;

    public VentaNegocio(VentaDatos ventas, ProductoDatos productos, ArticuloDatos articulos)
    {
        _ventas = ventas;
        _productos = productos;
        _articulos = articulos;
    }

    // ---------- Catálogo para la pantalla de Vender ----------

    public List<Producto> ListarMenu() => _productos.Listar(soloActivos: true);

    // Artículos que se pueden vender sueltos (sodas, paletas, sabritas...)
    public List<Articulo> ListarSueltos() =>
        _articulos.Listar().Where(a => a.Activo && a.SeVendeSuelto).ToList();

    // Artículos que el cliente puede elegir para su platillo (tipo Comida)
    public List<Articulo> ListarElegibles() =>
        _articulos.Listar().Where(a => a.Activo && a.Tipo == "Comida").ToList();

    public List<VentaResumen> ListarPorCorte(int corteId) => _ventas.ListarPorCorte(corteId);

    public TicketVenta? ObtenerTicket(int ventaId) => _ventas.ObtenerTicket(ventaId);

    // ---------- Operaciones ----------

    public Resultado Registrar(NuevaVenta? v)
    {
        if (v == null)
            return Resultado.Error("No llegó la información de la venta.");

        string? error = Validar(v);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            int ventaId = _ventas.Registrar(v, Sesion.UsuarioId);
            string texto = v.Tipo == "ConsumoPersonal" ? "Consumo personal" : "Venta";
            return Resultado.Ok($"{texto} #{ventaId} registrada.", ventaId);
        }
        catch (MySqlException ex)
        {
            // Los SIGNAL del procedimiento y los triggers llegan aquí con su mensaje
            // (ej. "Stock insuficiente de Sonny: hay 2 y se necesitan 5")
            return Resultado.Error(ex.Message);
        }
    }

    public Resultado Cancelar(int ventaId, string? motivo)
    {
        motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (motivo != null && motivo.Length > 200)
            motivo = motivo[..200];

        try
        {
            _ventas.Cancelar(ventaId, Sesion.UsuarioId, motivo);
            return Resultado.Ok($"Venta #{ventaId} cancelada. Lo que salió del inventario ya regresó.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error(ex.Message);
        }
    }

    // ---------- Reglas ----------

    private static string? Validar(NuevaVenta v)
    {
        // Limpieza
        v.NombreOrden = string.IsNullOrWhiteSpace(v.NombreOrden) ? null : v.NombreOrden.Trim();
        v.MotivoDescuento = string.IsNullOrWhiteSpace(v.MotivoDescuento) ? null : v.MotivoDescuento.Trim();
        v.Items ??= new();
        v.Pagos ??= new();
        foreach (var i in v.Items)
        {
            i.Elegidos ??= new();
            i.Nota = string.IsNullOrWhiteSpace(i.Nota) ? null : i.Nota.Trim();
        }

        if (v.Tipo != "Venta" && v.Tipo != "ConsumoPersonal")
            return "Tipo de venta no válido.";

        if (v.NombreOrden?.Length > 50)
            return "El nombre de la orden no puede pasar de 50 caracteres.";

        if (v.Items.Count == 0)
            return "La orden está vacía.";

        foreach (var i in v.Items)
        {
            if (i.Tipo != "producto" && i.Tipo != "articulo")
                return "Hay un renglón no válido en la orden.";
            if (i.Cantidad <= 0 || i.Cantidad > 100)
                return "Las cantidades deben ser de 1 a 100.";
            if (i.Nota?.Length > 150)
                return "Cada nota puede tener máximo 150 caracteres.";
            if (i.Elegidos.Any(e => e.Cantidad <= 0))
                return "Las cantidades de lo elegido deben ser mayores a 0.";
        }

        // El consumo personal no lleva pagos ni descuento (CK_Venta_Consumo)
        if (v.Tipo == "ConsumoPersonal")
        {
            v.Pagos.Clear();
            v.Descuento = 0;
            v.MotivoDescuento = null;
            v.EfectivoRecibido = null;
            return null;
        }

        if (v.Descuento < 0)
            return "El descuento no puede ser negativo.";
        if (v.Descuento > 0 && v.MotivoDescuento == null)
            return "Escribe el motivo del descuento (ej. Familiar).";
        if (v.Descuento == 0)
            v.MotivoDescuento = null;
        if (v.MotivoDescuento?.Length > 100)
            return "El motivo del descuento no puede pasar de 100 caracteres.";

        // Solo los pagos con monto, y uno por método (UQ_Pago_Venta_Metodo)
        v.Pagos = v.Pagos.Where(p => p.Monto > 0).ToList();

        if (v.Pagos.Count == 0)
            return "Captura cómo pagó el cliente.";
        if (v.Pagos.Any(p => !Metodos.Contains(p.Metodo)))
            return "Método de pago no válido.";
        if (v.Pagos.Select(p => p.Metodo).Distinct().Count() != v.Pagos.Count)
            return "No repitas el mismo método de pago.";

        decimal efectivo = v.Pagos.Where(p => p.Metodo == "Efectivo").Sum(p => p.Monto);
        if (efectivo == 0)
            v.EfectivoRecibido = null;
        else if (v.EfectivoRecibido.HasValue && v.EfectivoRecibido < efectivo)
            return "El efectivo recibido es menor a lo que se paga en efectivo.";

        return null;
    }
}
