namespace MaizitosBaja.Web;

// Datos que salen en el ticket. Se capturan en appsettings.json
// (sección "Negocio"), así se cambian sin tocar el código.
// Los que se dejen vacíos no se imprimen.
public class DatosNegocio
{
    public string Nombre { get; set; } = "Maizitos Baja";
    public string? Lema { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TikTok { get; set; }
    public string Agradecimiento { get; set; } = "¡Gracias por su compra!";
}
