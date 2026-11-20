using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Caja;

public class IndexModel : PageModel
{
    private readonly CajaNegocio _caja;
    private readonly VentaNegocio _ventas;

    public CorteCaja? Abierta { get; private set; }
    public CorteCaja? Cerrada { get; private set; }     // la que se acaba de cerrar
    public List<VentaResumen> Ventas { get; private set; } = new();
    public string? Error { get; private set; }

    public IndexModel(CajaNegocio caja, VentaNegocio ventas)
    {
        _caja = caja;
        _ventas = ventas;
    }

    // ?cerrado=5 muestra el resumen del corte que se acaba de cerrar
    public void OnGet(int? cerrado)
    {
        try
        {
            Abierta = _caja.ObtenerAbierta();

            if (Abierta != null)
                Ventas = _ventas.ListarPorCorte(Abierta.CorteId);
            else if (cerrado.HasValue)
            {
                Cerrada = _caja.ObtenerCorte(cerrado.Value);
                if (Cerrada != null)
                    Ventas = _ventas.ListarPorCorte(Cerrada.CorteId);
            }
        }
        catch (Exception ex)
        {
            Error = "No se pudo cargar la caja: " + ex.Message;
        }
    }

    public IActionResult OnPostAbrir(decimal fondoInicial)
    {
        var r = _caja.Abrir(fondoInicial);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return r.Exito ? RedirectToPage("/Vender/Index") : RedirectToPage();
    }

    public IActionResult OnPostCerrar(decimal efectivoContado, string? observaciones)
    {
        var r = _caja.Cerrar(efectivoContado, observaciones);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return r.Exito ? RedirectToPage(new { cerrado = r.Id }) : RedirectToPage();
    }

    public IActionResult OnPostCancelar(int ventaId, string? motivo)
    {
        var r = _ventas.Cancelar(ventaId, motivo);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return RedirectToPage();
    }
}
