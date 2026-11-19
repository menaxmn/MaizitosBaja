using MaizitosBaja.Datos;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace MaizitosBaja.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ConexionBD _conexion;

    public string Mensaje { get; private set; } = "";
    public bool Conectado { get; private set; }

    public IndexModel(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    public void OnGet()
    {
        try
        {
            using var cn = _conexion.CrearConexion();
            cn.Open();

            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Articulo", cn);
            int total = Convert.ToInt32(cmd.ExecuteScalar());

            Conectado = true;
            Mensaje = $"Conectado a MySQL {cn.ServerVersion}. Artículos en inventario: {total}";
        }
        catch (Exception ex)
        {
            Mensaje = "No se pudo conectar: " + ex.Message;
        }
    }
}