using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

// Movimientos de inventario. Aquí solo se INSERTA en MovimientoInventario;
// los triggers validan que no quede stock negativo y actualizan Articulo.Stock.
public class MovimientoDatos
{
    private readonly ConexionBD _conexion;

    private const string SqlInsertar = @"
        INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, ProveedorId, Observaciones)
        VALUES (@articulo, @tipo, @cantidad, @usuario, @proveedor, @obs)";

    public MovimientoDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    // Entrada de varios artículos (una compra). Todo en una transacción:
    // si un renglón falla, no se guarda ninguno.
    public void RegistrarEntrada(List<RenglonEntrada> renglones, int? proveedorId, string? observaciones, int usuarioId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            foreach (var r in renglones)
            {
                using var cmd = new MySqlCommand(SqlInsertar, cn, tx);
                cmd.Parameters.AddWithValue("@articulo", r.ArticuloId);
                cmd.Parameters.AddWithValue("@tipo", "Entrada");
                cmd.Parameters.AddWithValue("@cantidad", r.Cantidad);
                cmd.Parameters.AddWithValue("@usuario", usuarioId);
                cmd.Parameters.AddWithValue("@proveedor", (object?)proveedorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@obs", (object?)observaciones ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // Un solo movimiento (Uso, Merma o Ajuste). cantidad ya trae su signo.
    public void Registrar(int articuloId, string tipo, int cantidad, string? observaciones, int usuarioId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(SqlInsertar, cn);
        cmd.Parameters.AddWithValue("@articulo", articuloId);
        cmd.Parameters.AddWithValue("@tipo", tipo);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@usuario", usuarioId);
        cmd.Parameters.AddWithValue("@proveedor", DBNull.Value);
        cmd.Parameters.AddWithValue("@obs", (object?)observaciones ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // Historial con filtros opcionales (null = sin filtro). Máximo 500 renglones.
    public List<MovimientoInventario> Listar(DateTime? desde, DateTime? hasta, string? tipo, int? articuloId)
    {
        string sql = @"
            SELECT m.MovimientoId, m.ArticuloId, TRIM(CONCAT(a.Nombre, ' ', a.Variante)) AS Articulo,
                   c.Nombre AS Categoria, m.Tipo, m.Cantidad, m.StockAnterior, m.StockNuevo,
                   m.Fecha, m.ProveedorId, p.Nombre AS Proveedor, m.VentaId, m.Observaciones
              FROM MovimientoInventario m
              JOIN Articulo a       ON a.ArticuloId  = m.ArticuloId
              JOIN Categoria c      ON c.CategoriaId = a.CategoriaId
              LEFT JOIN Proveedor p ON p.ProveedorId = m.ProveedorId
             WHERE (@desde IS NULL OR m.Fecha >= @desde)
               AND (@hasta IS NULL OR m.Fecha <  @hasta)
               AND (@tipo  IS NULL OR m.Tipo = @tipo)
               AND (@art   IS NULL OR m.ArticuloId = @art)
             ORDER BY m.Fecha DESC, m.MovimientoId DESC
             LIMIT 500";

        var lista = new List<MovimientoInventario>();

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hasta", (object?)hasta?.Date.AddDays(1) ?? DBNull.Value);   // incluye todo el día "hasta"
        cmd.Parameters.AddWithValue("@tipo", (object?)tipo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@art", (object?)articuloId ?? DBNull.Value);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            lista.Add(new MovimientoInventario
            {
                MovimientoId  = Convert.ToInt32(r["MovimientoId"]),
                ArticuloId    = Convert.ToInt32(r["ArticuloId"]),
                Articulo      = Convert.ToString(r["Articulo"]) ?? "",
                Categoria     = Convert.ToString(r["Categoria"]) ?? "",
                Tipo          = Convert.ToString(r["Tipo"]) ?? "",
                Cantidad      = Convert.ToInt32(r["Cantidad"]),
                StockAnterior = r["StockAnterior"] == DBNull.Value ? null : Convert.ToInt32(r["StockAnterior"]),
                StockNuevo    = r["StockNuevo"] == DBNull.Value ? null : Convert.ToInt32(r["StockNuevo"]),
                Fecha         = Convert.ToDateTime(r["Fecha"]),
                ProveedorId   = r["ProveedorId"] == DBNull.Value ? null : Convert.ToInt32(r["ProveedorId"]),
                Proveedor     = r["Proveedor"] == DBNull.Value ? null : Convert.ToString(r["Proveedor"]),
                VentaId       = r["VentaId"] == DBNull.Value ? null : Convert.ToInt32(r["VentaId"]),
                Observaciones = r["Observaciones"] == DBNull.Value ? null : Convert.ToString(r["Observaciones"])
            });
        }
        return lista;
    }
}
