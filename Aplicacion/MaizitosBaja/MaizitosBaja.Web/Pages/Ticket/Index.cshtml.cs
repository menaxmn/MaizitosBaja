using MaizitosBaja.Entidades;
using MaizitosBaja.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Ticket;

// Ticket de una venta, con medidas de rollo térmico de 80 mm.
// /Ticket/12            -> muestra el ticket
// /Ticket/12?imprimir=1 -> además abre el diálogo de impresión
public class IndexModel : PageModel
{
    private readonly VentaNegocio _ventas;

    public DatosNegocio Negocio { get; }
    public TicketVenta? Venta { get; private set; }
    public bool Imprimir { get; private set; }

    public IndexModel(VentaNegocio ventas, DatosNegocio negocio)
    {
        _ventas = ventas;
        Negocio = negocio;
    }

    public IActionResult OnGet(int id, bool imprimir = false)
    {
        Venta = _ventas.ObtenerTicket(id);
        if (Venta == null)
            return NotFound("La venta no existe.");

        Imprimir = imprimir;
        return Page();
    }
}
