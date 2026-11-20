using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly ProductoNegocio _negocio;

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool VerInactivos { get; set; }

    public List<Producto> Productos { get; private set; } = new();
    public string? Error { get; private set; }

    public IndexModel(ProductoNegocio negocio)
    {
        _negocio = negocio;
    }

    public void OnGet()
    {
        try
        {
            Productos = _negocio.Listar(Buscar, VerInactivos);
        }
        catch (Exception ex)
        {
            Error = "No se pudo cargar el menú: " + ex.Message;
        }
    }

    public IActionResult OnPostCambiarEstado(int id)
    {
        var r = _negocio.CambiarEstado(id);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return RedirectToPage(new { Buscar, VerInactivos });
    }
}
