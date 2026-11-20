using System.Text.Json;
using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Movimientos;

// Movimientos de inventario en 4 pestañas:
// ?pestana=entrada | salida | ajuste | historial
public class IndexModel : PageModel
{
    private readonly MovimientoNegocio _negocio;

    [BindProperty(SupportsGet = true)]
    public string Pestana { get; set; } = "entrada";

    // Artículo preseleccionado (viene de Inventario) o filtro del historial
    [BindProperty(SupportsGet = true)]
    public int? ArticuloId { get; set; }

    // Filtros del historial
    [BindProperty(SupportsGet = true)]
    public DateTime? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Hasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tipo { get; set; }

    public List<Articulo> Articulos { get; private set; } = new();         // activos, para registrar
    public List<Articulo> TodosLosArticulos { get; private set; } = new(); // para filtrar el historial
    public List<Proveedor> Proveedores { get; private set; } = new();
    public List<MovimientoInventario> Historial { get; private set; } = new();
    public string? Error { get; private set; }

    // Para no perder lo capturado en la entrada si hubo un error
    public string RenglonesJson { get; private set; } = "[]";
    public int? ProveedorPrevio { get; private set; }
    public string? ObservacionesPrevias { get; private set; }

    public static readonly string[] Pestanas = { "entrada", "salida", "ajuste", "historial" };

    public static readonly Dictionary<string, string> NombresTipo = new()
    {
        ["Inicial"] = "Inicial",
        ["Entrada"] = "Entrada",
        ["Venta"] = "Venta",
        ["ConsumoPersonal"] = "Consumo personal",
        ["Uso"] = "Uso",
        ["Merma"] = "Merma",
        ["Ajuste"] = "Ajuste",
        ["Cancelacion"] = "Cancelación"
    };

    public IndexModel(MovimientoNegocio negocio)
    {
        _negocio = negocio;
    }

    public void OnGet()
    {
        if (!Pestanas.Contains(Pestana))
            Pestana = "entrada";

        // Historial: por defecto los últimos 7 días; si se pide un artículo, todo su kardex
        if (Pestana == "historial" && Desde == null && Hasta == null && ArticuloId == null)
            Desde = DateTime.Today.AddDays(-7);

        Cargar();
    }

    public IActionResult OnPostEntrada(List<RenglonEntrada>? renglones, int? proveedorId, string? observaciones)
    {
        var r = _negocio.RegistrarEntrada(renglones ?? new(), proveedorId, observaciones);
        if (r.Exito)
        {
            TempData["Mensaje"] = r.Mensaje;
            return RedirectToPage(new { pestana = "entrada" });
        }

        // Regresa a la misma pantalla con lo que ya se había capturado
        Pestana = "entrada";
        Error = r.Mensaje;
        RenglonesJson = JsonSerializer.Serialize(
            (renglones ?? new()).Select(x => new { articuloId = x.ArticuloId, cantidad = x.Cantidad }));
        ProveedorPrevio = proveedorId;
        ObservacionesPrevias = observaciones;
        Cargar();
        return Page();
    }

    public IActionResult OnPostSalida(int articuloId, string? tipoSalida, int cantidad, string? motivo)
    {
        var r = _negocio.RegistrarSalida(articuloId, tipoSalida, cantidad, motivo);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return RedirectToPage(new { pestana = "salida", articuloId = r.Exito ? (int?)null : articuloId });
    }

    public IActionResult OnPostAjuste(int articuloId, int conteo, string? motivo)
    {
        var r = _negocio.RegistrarAjuste(articuloId, conteo, motivo);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return RedirectToPage(new { pestana = "ajuste", articuloId = r.Exito ? (int?)null : articuloId });
    }

    private void Cargar()
    {
        try
        {
            Articulos = _negocio.ListarArticulosActivos();
            Proveedores = _negocio.ListarProveedoresActivos();

            if (Pestana == "historial")
            {
                TodosLosArticulos = _negocio.ListarTodosLosArticulos();
                Historial = _negocio.Historial(Desde, Hasta, Tipo, ArticuloId);
            }
        }
        catch (Exception ex)
        {
            Error = "No se pudo cargar la información: " + ex.Message;
        }
    }
}
