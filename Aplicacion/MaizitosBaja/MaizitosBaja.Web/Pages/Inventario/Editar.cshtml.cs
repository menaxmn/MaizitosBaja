using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;

namespace MaizitosBaja.Web.Pages.Inventario;

public class EditarModel : ArticuloFormPageModel
{
    public EditarModel(ArticuloNegocio negocio) : base(negocio) { }

    public IActionResult OnGet(int id)
    {
        var articulo = _negocio.Obtener(id);
        if (articulo == null)
        {
            TempData["Error"] = "El artículo no existe.";
            return RedirectToPage("./Index");
        }

        Input = ArticuloInput.DesdeEntidad(articulo);
        CargarListas();
        return Page();
    }

    public IActionResult OnPost(int id)
    {
        Input.ArticuloId = id;

        if (!ModelState.IsValid)
        {
            RecargarExistencia(id);
            CargarListas();
            return Page();
        }

        var resultado = _negocio.Actualizar(Input.ToEntidad());

        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            RecargarExistencia(id);
            CargarListas();
            return Page();
        }

        TempData["Mensaje"] = resultado.Mensaje;
        return RedirectToPage("./Index");
    }

    // La existencia no viaja en el formulario; se vuelve a leer
    private void RecargarExistencia(int id)
    {
        Input.StockActual = _negocio.Obtener(id)?.Stock ?? 0;
    }
}
