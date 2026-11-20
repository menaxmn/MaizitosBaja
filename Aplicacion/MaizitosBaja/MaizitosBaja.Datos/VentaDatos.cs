using System.Data;
using System.Text.Json;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

// Ventas. Registrar y cancelar se hacen con los procedimientos
// sp_RegistrarVenta y sp_CancelarVenta (ellos manejan la transacción
// y los triggers descuentan / regresan el inventario).
public class VentaDatos
{
    private readonly ConexionBD _conexion;

    public VentaDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    // Devuelve el VentaId nuevo
    public int Registrar(NuevaVenta v, int usuarioId)
    {
        // JSON con el formato que espera el procedimiento:
        // [{"tipo":"producto","id":18,"cantidad":1,"nota":"sin chile","elegidos":[{"id":17,"cantidad":1}]}]
        string items = JsonSerializer.Serialize(v.Items.Select(i => new
        {
            tipo = i.Tipo,
            id = i.Id,
            cantidad = i.Cantidad,
            nota = i.Nota,
            elegidos = i.Elegidos.Select(e => new { id = e.Id, cantidad = e.Cantidad })
        }));

        // [{"metodo":"Tarjeta","monto":100},{"metodo":"Efectivo","monto":75}]
        string pagos = JsonSerializer.Serialize(v.Pagos.Select(p => new
        {
            metodo = p.Metodo,
            monto = p.Monto
        }));

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand("sp_RegistrarVenta", cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@p_UsuarioId", usuarioId);
        cmd.Parameters.AddWithValue("@p_NombreOrden", (object?)v.NombreOrden ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p_Tipo", v.Tipo);
        cmd.Parameters.AddWithValue("@p_Descuento", v.Descuento);
        cmd.Parameters.AddWithValue("@p_MotivoDescuento", (object?)v.MotivoDescuento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p_EfectivoRecibido", (object?)v.EfectivoRecibido ?? DBNull.Value);
        cmd.Parameters.Add(new MySqlParameter("@p_Items", MySqlDbType.JSON) { Value = items });
        cmd.Parameters.Add(new MySqlParameter("@p_Pagos", MySqlDbType.JSON) { Value = pagos });
        var salida = new MySqlParameter("@p_VentaId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(salida);

        cmd.ExecuteNonQuery();
        return Convert.ToInt32(salida.Value);
    }

    public void Cancelar(int ventaId, int usuarioId, string? motivo)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand("sp_CancelarVenta", cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@p_VentaId", ventaId);
        cmd.Parameters.AddWithValue("@p_UsuarioId", usuarioId);
        cmd.Parameters.AddWithValue("@p_Motivo", (object?)motivo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // Todo lo necesario para imprimir el ticket (null si la venta no existe)
    public TicketVenta? ObtenerTicket(int ventaId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();

        TicketVenta t;

        // 1. Encabezado
        using (var cmd = new MySqlCommand(@"
            SELECT VentaId, Fecha, NombreOrden, Tipo, Subtotal, Descuento, MotivoDescuento,
                   Total, EfectivoRecibido, Cambio, Estado
              FROM Venta
             WHERE VentaId = @id", cn))
        {
            cmd.Parameters.AddWithValue("@id", ventaId);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return null;

            t = new TicketVenta
            {
                VentaId          = Convert.ToInt32(r["VentaId"]),
                Fecha            = Convert.ToDateTime(r["Fecha"]),
                NombreOrden      = r["NombreOrden"] == DBNull.Value ? null : Convert.ToString(r["NombreOrden"]),
                Tipo             = Convert.ToString(r["Tipo"]) ?? "",
                Subtotal         = Convert.ToDecimal(r["Subtotal"]),
                Descuento        = Convert.ToDecimal(r["Descuento"]),
                MotivoDescuento  = r["MotivoDescuento"] == DBNull.Value ? null : Convert.ToString(r["MotivoDescuento"]),
                Total            = Convert.ToDecimal(r["Total"]),
                EfectivoRecibido = r["EfectivoRecibido"] == DBNull.Value ? null : Convert.ToDecimal(r["EfectivoRecibido"]),
                Cambio           = r["Cambio"] == DBNull.Value ? null : Convert.ToDecimal(r["Cambio"]),
                Estado           = Convert.ToString(r["Estado"]) ?? ""
            };
        }

        // 2. Renglones, con lo que eligió el cliente y la nota
        using (var cmd = new MySqlCommand(@"
            SELECT d.Cantidad, d.PrecioUnitario, d.Subtotal, d.Nota,
                   COALESCE(p.Nombre, TRIM(CONCAT(a.Nombre, ' ', a.Variante))) AS Concepto,
                   (SELECT GROUP_CONCAT(CONCAT(IF(dva.Cantidad > 1, CONCAT(dva.Cantidad, 'x '), ''),
                                               TRIM(CONCAT(ae.Nombre, ' ', ae.Variante)))
                                        SEPARATOR ', ')
                      FROM DetalleVentaArticulo dva
                      JOIN Articulo ae ON ae.ArticuloId = dva.ArticuloId
                     WHERE dva.DetalleVentaId = d.DetalleVentaId) AS Elegidos
              FROM DetalleVenta d
              LEFT JOIN Producto p ON p.ProductoId = d.ProductoId
              LEFT JOIN Articulo a ON a.ArticuloId = d.ArticuloId
             WHERE d.VentaId = @id
             ORDER BY d.DetalleVentaId", cn))
        {
            cmd.Parameters.AddWithValue("@id", ventaId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                t.Renglones.Add(new TicketRenglon
                {
                    Cantidad       = Convert.ToInt32(r["Cantidad"]),
                    PrecioUnitario = Convert.ToDecimal(r["PrecioUnitario"]),
                    Subtotal       = Convert.ToDecimal(r["Subtotal"]),
                    Nota           = r["Nota"] == DBNull.Value ? null : Convert.ToString(r["Nota"]),
                    Concepto       = Convert.ToString(r["Concepto"]) ?? "",
                    Elegidos       = r["Elegidos"] == DBNull.Value ? null : Convert.ToString(r["Elegidos"])
                });
            }
        }

        // 3. Pagos
        using (var cmd = new MySqlCommand(@"
            SELECT MetodoPago, Monto
              FROM Pago
             WHERE VentaId = @id
             ORDER BY FIELD(MetodoPago, 'Efectivo', 'Tarjeta', 'Transferencia')", cn))
        {
            cmd.Parameters.AddWithValue("@id", ventaId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                t.Pagos.Add(new PagoVenta
                {
                    Metodo = Convert.ToString(r["MetodoPago"]) ?? "",
                    Monto  = Convert.ToDecimal(r["Monto"])
                });
            }
        }

        return t;
    }

    // Ventas de un corte, la más reciente primero
    public List<VentaResumen> ListarPorCorte(int corteId)
    {
        string sql = @"
            SELECT v.VentaId, v.Fecha, v.NombreOrden, v.Tipo, v.Descuento, v.Total, v.Cambio, v.Estado,
                   (SELECT GROUP_CONCAT(CONCAT(d.Cantidad, ' ',
                               COALESCE(p.Nombre, TRIM(CONCAT(a.Nombre, ' ', a.Variante))),
                               IF(d.Nota IS NULL, '', CONCAT(' (', d.Nota, ')')))
                           ORDER BY d.DetalleVentaId SEPARATOR ', ')
                      FROM DetalleVenta d
                      LEFT JOIN Producto p ON p.ProductoId = d.ProductoId
                      LEFT JOIN Articulo a ON a.ArticuloId = d.ArticuloId
                     WHERE d.VentaId = v.VentaId) AS Conceptos,
                   (SELECT GROUP_CONCAT(CONCAT(pg.MetodoPago, ' $', pg.Monto) SEPARATOR ' + ')
                      FROM Pago pg
                     WHERE pg.VentaId = v.VentaId) AS Pagos
              FROM Venta v
             WHERE v.CorteId = @corte
             ORDER BY v.Fecha DESC, v.VentaId DESC";

        var lista = new List<VentaResumen>();

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@corte", corteId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            lista.Add(new VentaResumen
            {
                VentaId     = Convert.ToInt32(r["VentaId"]),
                Fecha       = Convert.ToDateTime(r["Fecha"]),
                NombreOrden = r["NombreOrden"] == DBNull.Value ? null : Convert.ToString(r["NombreOrden"]),
                Tipo        = Convert.ToString(r["Tipo"]) ?? "",
                Descuento   = Convert.ToDecimal(r["Descuento"]),
                Total       = Convert.ToDecimal(r["Total"]),
                Cambio      = r["Cambio"] == DBNull.Value ? null : Convert.ToDecimal(r["Cambio"]),
                Estado      = Convert.ToString(r["Estado"]) ?? "",
                Conceptos   = r["Conceptos"] == DBNull.Value ? "" : Convert.ToString(r["Conceptos"]) ?? "",
                Pagos       = r["Pagos"] == DBNull.Value ? "" : Convert.ToString(r["Pagos"]) ?? ""
            });
        }
        return lista;
    }
}
