using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Proveedores;

public class CrearModel : PageModel
{
    private readonly ProveedorNegocio _negocio;

    [BindProperty]
    public ProveedorInput Input { get; set; } = new();

    // Si viene de Movimientos, regresa allá al guardar
    [BindProperty(SupportsGet = true)]
    public string? Volver { get; set; }

    public CrearModel(ProveedorNegocio negocio)
    {
        _negocio = negocio;
    }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        var r = _negocio.Agregar(Input.ToEntidad());
        if (!r.Exito)
        {
            ModelState.AddModelError("Input", r.Mensaje); // clave = prefijo del partial (for="Input"), si no el resumen no la muestra
            return Page();
        }

        TempData["Mensaje"] = r.Mensaje;
        return Volver == "movimientos"
            ? RedirectToPage("/Movimientos/Index", new { pestana = "entrada" })
            : RedirectToPage("./Index");
    }
}
