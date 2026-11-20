using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Productos;

public class CrearModel : PageModel
{
    private readonly ProductoNegocio _negocio;

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public CrearModel(ProductoNegocio negocio)
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
            ModelState.AddModelError(string.Empty, r.Mensaje);
            return Page();
        }

        TempData["Mensaje"] = r.Mensaje;
        return RedirectToPage("./Index");
    }
}
