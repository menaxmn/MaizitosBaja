using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Productos;

public class EditarModel : PageModel
{
    private readonly ProductoNegocio _negocio;

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public EditarModel(ProductoNegocio negocio)
    {
        _negocio = negocio;
    }

    public IActionResult OnGet(int id)
    {
        var p = _negocio.Obtener(id);
        if (p == null)
        {
            TempData["Error"] = "El platillo no existe.";
            return RedirectToPage("./Index");
        }

        Input = ProductoInput.DesdeEntidad(p);
        return Page();
    }

    public IActionResult OnPost(int id)
    {
        Input.ProductoId = id;

        if (!ModelState.IsValid)
            return Page();

        var r = _negocio.Actualizar(Input.ToEntidad());
        if (!r.Exito)
        {
            ModelState.AddModelError("Input", r.Mensaje); // clave = prefijo del partial (for="Input"), si no el resumen no la muestra
            return Page();
        }

        TempData["Mensaje"] = r.Mensaje;
        return RedirectToPage("./Index");
    }
}
