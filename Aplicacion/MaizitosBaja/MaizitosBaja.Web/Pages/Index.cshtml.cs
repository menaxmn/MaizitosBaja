using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages;

// Pantalla de Inicio. Cambia según el estado de la caja:
//  - Cerrada            -> botón grande para abrirla
//  - Abierta de hoy     -> resumen del día y lo que se está acabando
//  - Abierta de otro día -> aviso para hacer el corte
public class IndexModel : PageModel
{
    private readonly CajaNegocio _caja;
    private readonly VentaNegocio _ventas;
    private readonly ArticuloNegocio _articulos;

    public CorteCaja? Abierta { get; private set; }
    public CorteCaja? UltimoCerrado { get; private set; }
    public List<VentaResumen> UltimasVentas { get; private set; } = new();
    public List<Articulo> PorAcabarse { get; private set; } = new();
    public string? Error { get; private set; }

    public bool CorteDeHoyHecho => Abierta == null && UltimoCerrado?.EsDeHoy == true;

    public string Saludo => DateTime.Now.Hour switch
    {
        < 12 => "Buenos días",
        < 19 => "Buenas tardes",
        _ => "Buenas noches"
    };

    public IndexModel(CajaNegocio caja, VentaNegocio ventas, ArticuloNegocio articulos)
    {
        _caja = caja;
        _ventas = ventas;
        _articulos = articulos;
    }

    public void OnGet()
    {
        try
        {
            Abierta = _caja.ObtenerAbierta();

            if (Abierta == null)
            {
                UltimoCerrado = _caja.ObtenerUltimoCerrado();
            }
            else
            {
                UltimasVentas = _ventas.ListarPorCorte(Abierta.CorteId).Take(5).ToList();
            }

            // Lo que está agotado o por debajo de su mínimo, lo más urgente primero
            PorAcabarse = _articulos.Listar(null, null, incluirInactivos: false)
                .Where(a => a.EstadoStock != "OK")
                .OrderBy(a => a.Stock)
                .ThenBy(a => a.NombreCompleto)
                .Take(8)
                .ToList();
        }
        catch (Exception ex)
        {
            Error = "No se pudo conectar con la base de datos: " + ex.Message;
        }
    }

    public IActionResult OnPostAbrir(decimal fondoInicial)
    {
        var r = _caja.Abrir(fondoInicial);
        TempData[r.Exito ? "Mensaje" : "Error"] = r.Mensaje;
        return r.Exito ? RedirectToPage("/Vender/Index") : RedirectToPage();
    }
}
