using MySqlConnector;

namespace MaizitosBaja.Datos;

// Única clase que sabe cómo conectarse a MySQL.
// Las demás clases de Datos le piden una conexión a ella.
public class ConexionBD
{
    private readonly string _cadena;

    public ConexionBD(string cadena)
    {
        _cadena = cadena ?? "";
    }

    // Si falta la configuración, el error sale en pantalla (las páginas lo muestran)
    // en lugar de que el sistema no arranque.
    public MySqlConnection CrearConexion()
    {
        if (string.IsNullOrWhiteSpace(_cadena))
            throw new InvalidOperationException(
                "No se configuró la conexión a MySQL. Revisa appsettings.Production.json " +
                "(versión instalada) o los User Secrets (Visual Studio).");

        return new MySqlConnection(_cadena);
    }
}
