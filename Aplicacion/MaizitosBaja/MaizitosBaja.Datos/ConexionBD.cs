using MySqlConnector;

namespace MaizitosBaja.Datos;

// Única clase que sabe cómo conectarse a MySQL.
// Las demás clases de Datos le piden una conexión a ella.
public class ConexionBD
{
    private readonly string _cadena;

    public ConexionBD(string cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                "No se configuró la cadena de conexión 'MaizitosBaja'.");

        _cadena = cadena;
    }

    public MySqlConnection CrearConexion() => new MySqlConnection(_cadena);
}