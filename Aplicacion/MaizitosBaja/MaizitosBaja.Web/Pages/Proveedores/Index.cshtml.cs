using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Proveedores;

public class IndexModel : PageModel
{
    private readonly ProveedorNegocio _negocio;

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool VerInactivos { get; set; }

    public List<Proveedor> Proveedores { get; private set; } = new();
    public string? Error { get; private set; }

    public IndexModel(ProveedorNegocio negocio)
    {
        _negocio = negocio;
    }

    public void OnGet()
    {
        try
        {
            Proveedores = _negocio.Listar(Buscar, VerInactivos);
        }
        catch (Exception ex)
        {
            Error = "No se pudieron cargar los proveedores: " + ex.Message;
        }
    }

    public IActionResult OnPostCambiarEstado(int id)
    {
        var r = _negocio.CambiarEstado(id);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return RedirectToPage(new { Buscar, VerInactivos });
    }
}
