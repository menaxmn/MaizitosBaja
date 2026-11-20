using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MaizitosBaja.Web.Pages.Inventario;

// Base compartida por Crear y Editar: el formulario y sus listas.
public abstract class ArticuloFormPageModel : PageModel
{
    protected readonly ArticuloNegocio _negocio;

    [BindProperty]
    public ArticuloInput Input { get; set; } = new();

    public List<SelectListItem> Categorias { get; private set; } = new();
    public List<SelectListItem> Proveedores { get; private set; } = new();

    public bool EsNuevo => Input.ArticuloId == 0;

    protected ArticuloFormPageModel(ArticuloNegocio negocio)
    {
        _negocio = negocio;
    }

    // Llena los <select>. Las categorías salen agrupadas por tipo.
    protected void CargarListas()
    {
        var grupos = new Dictionary<string, SelectListGroup>();

        Categorias = _negocio.ListarCategorias()
            .Select(c =>
            {
                if (!grupos.TryGetValue(c.Tipo, out var grupo))
                {
                    grupo = new SelectListGroup { Name = c.Tipo };
                    grupos[c.Tipo] = grupo;
                }
                return new SelectListItem(c.Nombre, c.CategoriaId.ToString()) { Group = grupo };
            })
            .ToList();

        Proveedores = _negocio.ListarProveedores()
            .Select(p => new SelectListItem(p.Nombre, p.ProveedorId.ToString()))
            .ToList();
    }
}
