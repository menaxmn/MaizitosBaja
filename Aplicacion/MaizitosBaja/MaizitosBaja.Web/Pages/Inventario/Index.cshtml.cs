using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Inventario;

public class IndexModel : PageModel
{
    private readonly ArticuloNegocio _negocio;

    // Filtros (vienen en la URL: ?Tipo=Bebida&Buscar=coca)
    [BindProperty(SupportsGet = true)]
    public string? Tipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool VerInactivos { get; set; }

    public List<Articulo> Articulos { get; private set; } = new();

    // Contadores de arriba (solo artículos activos)
    public int TotalActivos { get; private set; }
    public int StockBajo { get; private set; }
    public int Agotados { get; private set; }

    public string? Error { get; private set; }

    public IndexModel(ArticuloNegocio negocio)
    {
        _negocio = negocio;
    }

    public void OnGet()
    {
        try
        {
            Articulos = _negocio.Listar(Tipo, Buscar, VerInactivos);

            var activos = _negocio.Listar(null, null, incluirInactivos: false);
            TotalActivos = activos.Count;
            StockBajo = activos.Count(a => a.EstadoStock == "Stock bajo");
            Agotados = activos.Count(a => a.EstadoStock == "Agotado");
        }
        catch (Exception ex)
        {
            Error = "No se pudo cargar el inventario: " + ex.Message;
        }
    }

    public IActionResult OnPostCambiarEstado(int id)
    {
        var resultado = _negocio.CambiarEstado(id);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Mensaje;

        // Regresa a la lista con los mismos filtros
        return RedirectToPage(new { Tipo, Buscar, VerInactivos });
    }
}
