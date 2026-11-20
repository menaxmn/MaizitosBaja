using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

// Acceso a la tabla Producto (platillos del menú).
public class ProductoDatos
{
    private readonly ConexionBD _conexion;

    private const string SelectBase =
        "SELECT ProductoId, Nombre, Descripcion, Precio, Activo FROM Producto";

    public ProductoDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    public List<Producto> Listar(bool soloActivos)
    {
        var lista = new List<Producto>();

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            SelectBase + " WHERE (@soloActivos = FALSE OR Activo = TRUE) ORDER BY Nombre", cn);
        cmd.Parameters.AddWithValue("@soloActivos", soloActivos);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(Mapear(r));

        return lista;
    }

    public Producto? ObtenerPorId(int id)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(SelectBase + " WHERE ProductoId = @id", cn);
        cmd.Parameters.AddWithValue("@id", id);

        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    // ¿Ya hay otro platillo con ese nombre? (UQ_Producto_Nombre)
    public bool ExisteNombre(string nombre, int excluirId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM Producto WHERE Nombre = @nombre AND ProductoId <> @id", cn);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@id", excluirId);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Insertar(Producto p)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(@"
            INSERT INTO Producto (Nombre, Descripcion, Precio, Activo)
            VALUES (@nombre, @descripcion, @precio, TRUE)", cn);
        AgregarParametros(cmd, p);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    // Cambiar el precio no afecta las ventas pasadas:
    // DetalleVenta guarda el precio con el que se vendió.
    public void Actualizar(Producto p)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(@"
            UPDATE Producto
               SET Nombre = @nombre, Descripcion = @descripcion, Precio = @precio
             WHERE ProductoId = @id", cn);
        AgregarParametros(cmd, p);
        cmd.Parameters.AddWithValue("@id", p.ProductoId);
        cmd.ExecuteNonQuery();
    }

    public void CambiarActivo(int id, bool activo)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            "UPDATE Producto SET Activo = @activo WHERE ProductoId = @id", cn);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static void AgregarParametros(MySqlCommand cmd, Producto p)
    {
        cmd.Parameters.AddWithValue("@nombre", p.Nombre);
        cmd.Parameters.AddWithValue("@descripcion", (object?)p.Descripcion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@precio", p.Precio);
    }

    private static Producto Mapear(MySqlDataReader r)
    {
        int iDesc = r.GetOrdinal("Descripcion");
        return new Producto
        {
            ProductoId  = r.GetInt32(r.GetOrdinal("ProductoId")),
            Nombre      = r.GetString(r.GetOrdinal("Nombre")),
            Descripcion = r.IsDBNull(iDesc) ? null : r.GetString(iDesc),
            Precio      = r.GetDecimal(r.GetOrdinal("Precio")),
            Activo      = r.GetBoolean(r.GetOrdinal("Activo"))
        };
    }
}
