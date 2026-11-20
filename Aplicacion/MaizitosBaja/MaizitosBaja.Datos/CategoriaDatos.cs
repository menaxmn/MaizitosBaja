using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

public class CategoriaDatos
{
    private readonly ConexionBD _conexion;

    public CategoriaDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    public List<Categoria> Listar(bool soloActivas)
    {
        var lista = new List<Categoria>();

        string sql = @"SELECT CategoriaId, Nombre, Tipo, Activo
                         FROM Categoria
                        WHERE (@soloActivas = FALSE OR Activo = TRUE)
                        ORDER BY Tipo, Nombre";

        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@soloActivas", soloActivas);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            lista.Add(new Categoria
            {
                CategoriaId = r.GetInt32(r.GetOrdinal("CategoriaId")),
                Nombre      = r.GetString(r.GetOrdinal("Nombre")),
                Tipo        = r.GetString(r.GetOrdinal("Tipo")),
                Activo      = r.GetBoolean(r.GetOrdinal("Activo"))
            });
        }
        return lista;
    }
}
