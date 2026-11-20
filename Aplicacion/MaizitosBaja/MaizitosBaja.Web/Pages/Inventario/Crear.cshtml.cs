using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;

namespace MaizitosBaja.Web.Pages.Inventario;

public class CrearModel : ArticuloFormPageModel
{
    public CrearModel(ArticuloNegocio negocio) : base(negocio) { }

    public void OnGet()
    {
        CargarListas();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            CargarListas();
            return Page();
        }

        var resultado = _negocio.Agregar(Input.ToEntidad(), Input.StockInicial);

        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            CargarListas();
            return Page();
        }

        TempData["Mensaje"] = resultado.Mensaje;
        return RedirectToPage("./Index");
    }
}
