using System.Text.Json;
using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Vender;

// Punto de venta. La orden se arma en el navegador (JavaScript)
// y al cobrar se manda completa a OnPostCobrar en formato JSON.
public class IndexModel : PageModel
{
    private readonly VentaNegocio _ventas;
    private readonly CajaNegocio _caja;

    public CorteCaja? Caja { get; private set; }
    public string CatalogoJson { get; private set; } = "{}";
    public string? Error { get; private set; }

    public IndexModel(VentaNegocio ventas, CajaNegocio caja)
    {
        _ventas = ventas;
        _caja = caja;
    }

    public void OnGet()
    {
        try
        {
            Caja = _caja.ObtenerAbierta();

            var catalogo = new
            {
                menu = _ventas.ListarMenu().Select(p => new
                {
                    id = p.ProductoId,
                    nombre = p.Nombre,
                    descripcion = p.Descripcion,
                    precio = p.Precio
                }),
                sueltos = _ventas.ListarSueltos().Select(a => new
                {
                    id = a.ArticuloId,
                    nombre = a.NombreCompleto,
                    categoria = a.Categoria,
                    precio = a.PrecioSuelto ?? 0,
                    stock = a.Stock
                }),
                elegibles = _ventas.ListarElegibles().Select(a => new
                {
                    id = a.ArticuloId,
                    nombre = a.NombreCompleto,
                    categoria = a.Categoria,
                    stock = a.Stock
                })
            };

            // El serializador escapa < > & ' así que es seguro meterlo en un <script>
            CatalogoJson = JsonSerializer.Serialize(catalogo, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (Exception ex)
        {
            Error = "No se pudo cargar el menú: " + ex.Message;
        }
    }

    public IActionResult OnPostCobrar([FromBody] NuevaVenta? venta)
    {
        var r = _ventas.Registrar(venta);
        return new JsonResult(new { exito = r.Exito, mensaje = r.Mensaje, ventaId = r.Id });
    }
}
