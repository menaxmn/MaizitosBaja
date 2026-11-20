using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

public class ProveedorDatos
{
    private readonly ConexionBD _conexion;

    private const string SelectBase =
        "SELECT ProveedorId, Nombre, Telefono, Correo, Activo FROM Proveedor";

    public ProveedorDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    public List<Proveedor> Listar(bool soloActivos)
    {
        var lista = new List<Proveedor>();

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

    public Proveedor? ObtenerPorId(int id)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(SelectBase + " WHERE ProveedorId = @id", cn);
        cmd.Parameters.AddWithValue("@id", id);

        using var r = cmd.ExecuteReader();
        return r.Read() ? Mapear(r) : null;
    }

    // ¿Ya hay otro proveedor con ese nombre? (UQ_Proveedor_Nombre)
    public bool ExisteNombre(string nombre, int excluirId)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM Proveedor WHERE Nombre = @nombre AND ProveedorId <> @id", cn);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@id", excluirId);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Insertar(Proveedor p)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(@"
            INSERT INTO Proveedor (Nombre, Telefono, Correo, Activo)
            VALUES (@nombre, @telefono, @correo, TRUE)", cn);
        AgregarParametros(cmd, p);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    public void Actualizar(Proveedor p)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(@"
            UPDATE Proveedor
               SET Nombre = @nombre, Telefono = @telefono, Correo = @correo
             WHERE ProveedorId = @id", cn);
        AgregarParametros(cmd, p);
        cmd.Parameters.AddWithValue("@id", p.ProveedorId);
        cmd.ExecuteNonQuery();
    }

    public void CambiarActivo(int id, bool activo)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(
            "UPDATE Proveedor SET Activo = @activo WHERE ProveedorId = @id", cn);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static void AgregarParametros(MySqlCommand cmd, Proveedor p)
    {
        cmd.Parameters.AddWithValue("@nombre", p.Nombre);
        cmd.Parameters.AddWithValue("@telefono", (object?)p.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@correo", (object?)p.Correo ?? DBNull.Value);
    }

    private static Proveedor Mapear(MySqlDataReader r)
    {
        int iTel = r.GetOrdinal("Telefono");
        int iCorreo = r.GetOrdinal("Correo");
        return new Proveedor
        {
            ProveedorId = r.GetInt32(r.GetOrdinal("ProveedorId")),
            Nombre      = r.GetString(r.GetOrdinal("Nombre")),
            Telefono    = r.IsDBNull(iTel) ? null : r.GetString(iTel),
            Correo      = r.IsDBNull(iCorreo) ? null : r.GetString(iCorreo),
            Activo      = r.GetBoolean(r.GetOrdinal("Activo"))
        };
    }
}
