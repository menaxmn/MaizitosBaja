using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

// Acceso a la tabla Articulo.
// OJO: aquí nunca se hace UPDATE a Stock. El stock solo cambia
// insertando en MovimientoInventario (los triggers lo actualizan).
public class ArticuloDatos
{
    private readonly ConexionBD _conexion;

    private const string SelectBase = @"
        SELECT a.ArticuloId, a.CategoriaId, a.ProveedorId, a.Nombre, a.Variante,
               a.Unidad, a.Stock, a.StockMinimo, a.SeVendeSuelto, a.PrecioSuelto,
               a.Activo, a.FechaRegistro,
               c.Nombre AS Categoria, c.Tipo, p.Nombre AS Proveedor
          FROM Articulo a
          JOIN Categoria c      ON c.CategoriaId = a.CategoriaId
          LEFT JOIN Proveedor p ON p.ProveedorId = a.ProveedorId";

    public ArticuloDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    public List<Articulo> Listar()
    {
        var lista = new List<Articulo>();

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            SelectBase + " ORDER BY c.Tipo, c.Nombre, a.Nombre, a.Variante", cn);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(Mapear(r));

        return lista;
    }

    public Articulo? ObtenerPorId(int id)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(SelectBase + " WHERE a.ArticuloId = @id", cn);
        cmd.Parameters.AddWithValue("@id", id);

        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    // ¿Ya existe otro artículo con la misma categoría, nombre y variante?
    // (la base lo impide con UQ_Articulo; aquí se revisa antes para dar un mensaje claro)
    public bool ExisteDuplicado(int categoriaId, string nombre, string variante, int excluirId)
    {
        string sql = @"SELECT COUNT(*) FROM Articulo
                        WHERE CategoriaId = @cat AND Nombre = @nombre
                          AND Variante = @variante AND ArticuloId <> @id";

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@cat", categoriaId);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@variante", variante);
        cmd.Parameters.AddWithValue("@id", excluirId);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // Da de alta el artículo con stock 0 y, si trae existencia,
    // registra el movimiento 'Inicial' (el trigger sube el stock).
    // Todo en una transacción: se guardan las dos cosas o ninguna.
    public int Insertar(Articulo a, int stockInicial, int usuarioId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            string sqlArticulo = @"
                INSERT INTO Articulo (CategoriaId, ProveedorId, Nombre, Variante, Unidad,
                                      StockMinimo, SeVendeSuelto, PrecioSuelto, Activo)
                VALUES (@cat, @prov, @nombre, @variante, @unidad,
                        @min, @suelto, @precio, TRUE)";

            using var cmd = new MySqlCommand(sqlArticulo, cn, tx);
            AgregarParametros(cmd, a);
            cmd.ExecuteNonQuery();
            int nuevoId = (int)cmd.LastInsertedId;

            if (stockInicial > 0)
            {
                string sqlMov = @"
                    INSERT INTO MovimientoInventario (ArticuloId, Tipo, Cantidad, UsuarioId, Observaciones)
                    VALUES (@id, 'Inicial', @cant, @usuario, 'Alta del artículo')";

                using var cmdMov = new MySqlCommand(sqlMov, cn, tx);
                cmdMov.Parameters.AddWithValue("@id", nuevoId);
                cmdMov.Parameters.AddWithValue("@cant", stockInicial);
                cmdMov.Parameters.AddWithValue("@usuario", usuarioId);
                cmdMov.ExecuteNonQuery();
            }

            tx.Commit();
            return nuevoId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // Edita los datos del artículo (NO el stock)
    public void Actualizar(Articulo a)
    {
        string sql = @"
            UPDATE Articulo
               SET CategoriaId   = @cat,
                   ProveedorId   = @prov,
                   Nombre        = @nombre,
                   Variante      = @variante,
                   Unidad        = @unidad,
                   StockMinimo   = @min,
                   SeVendeSuelto = @suelto,
                   PrecioSuelto  = @precio
             WHERE ArticuloId = @id";

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        AgregarParametros(cmd, a);
        cmd.Parameters.AddWithValue("@id", a.ArticuloId);
        cmd.ExecuteNonQuery();
    }

    // Activar / desactivar (productos de temporada). Nunca se borra.
    public void CambiarActivo(int id, bool activo)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            "UPDATE Articulo SET Activo = @activo WHERE ArticuloId = @id", cn);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static void AgregarParametros(MySqlCommand cmd, Articulo a)
    {
        cmd.Parameters.AddWithValue("@cat", a.CategoriaId);
        cmd.Parameters.AddWithValue("@prov", (object?)a.ProveedorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@nombre", a.Nombre);
        cmd.Parameters.AddWithValue("@variante", a.Variante);
        cmd.Parameters.AddWithValue("@unidad", a.Unidad);
        cmd.Parameters.AddWithValue("@min", a.StockMinimo);
        cmd.Parameters.AddWithValue("@suelto", a.SeVendeSuelto);
        cmd.Parameters.AddWithValue("@precio", (object?)a.PrecioSuelto ?? DBNull.Value);
    }

    private static Articulo Mapear(MySqlDataReader r)
    {
        int iProv = r.GetOrdinal("ProveedorId");
        int iPrecio = r.GetOrdinal("PrecioSuelto");
        int iNomProv = r.GetOrdinal("Proveedor");

        return new Articulo
        {
            ArticuloId    = r.GetInt32(r.GetOrdinal("ArticuloId")),
            CategoriaId   = r.GetInt32(r.GetOrdinal("CategoriaId")),
            ProveedorId   = r.IsDBNull(iProv) ? null : r.GetInt32(iProv),
            Nombre        = r.GetString(r.GetOrdinal("Nombre")),
            Variante      = r.GetString(r.GetOrdinal("Variante")),
            Unidad        = r.GetString(r.GetOrdinal("Unidad")),
            Stock         = r.GetInt32(r.GetOrdinal("Stock")),
            StockMinimo   = r.GetInt32(r.GetOrdinal("StockMinimo")),
            SeVendeSuelto = r.GetBoolean(r.GetOrdinal("SeVendeSuelto")),
            PrecioSuelto  = r.IsDBNull(iPrecio) ? null : r.GetDecimal(iPrecio),
            Activo        = r.GetBoolean(r.GetOrdinal("Activo")),
            FechaRegistro = r.GetDateTime(r.GetOrdinal("FechaRegistro")),
            Categoria     = r.GetString(r.GetOrdinal("Categoria")),
            Tipo          = r.GetString(r.GetOrdinal("Tipo")),
            Proveedor     = r.IsDBNull(iNomProv) ? null : r.GetString(iNomProv)
        };
    }
}
