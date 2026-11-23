using Microsoft.AspNetCore.Html;

namespace MaizitosBaja.Web;

// Íconos de línea del sistema (mismo estilo en la barra lateral, menús y pestañas).
// Se pintan con el color del texto (currentColor).
// Uso en una vista:  @Iconos.Svg("entrada")
public static class Iconos
{
    private static readonly Dictionary<string, string> Trazos = new()
    {
        // Barra lateral
        ["inicio"] = "<path d='M3 10.5 12 3l9 7.5'/><path d='M5 9.5V21h14V9.5'/><path d='M9.5 21v-6h5v6'/>",
        ["vender"] = "<circle cx='9' cy='20' r='1.5'/><circle cx='18' cy='20' r='1.5'/><path d='M2.5 3h3l2.4 11.2a1.5 1.5 0 0 0 1.5 1.2h8.4a1.5 1.5 0 0 0 1.5-1.1L21 7H6.3'/>",
        ["caja"] = "<rect x='2.5' y='6' width='19' height='12' rx='2'/><circle cx='12' cy='12' r='2.5'/><path d='M6 12h.01M18 12h.01'/>",
        ["productos"] = "<path d='M7 3v8a2 2 0 0 0 2 2v8'/><path d='M5 3v5a2 2 0 0 0 2 2'/><path d='M11 3v5a2 2 0 0 1-2 2'/><path d='M17 21V3c-2 1.5-3 4-3 7v3h3'/>",
        ["inventario"] = "<path d='M21 8 12 3 3 8v8l9 5 9-5z'/><path d='M3 8l9 5 9-5'/><path d='M12 13v8'/>",
        ["movimientos"] = "<path d='M7 4 3 8l4 4'/><path d='M3 8h13'/><path d='M17 20l4-4-4-4'/><path d='M21 16H8'/>",
        ["proveedores"] = "<path d='M2.5 6h11v10h-11z'/><path d='M13.5 9.5h4l3 3.5V16h-7'/><circle cx='7' cy='17.5' r='1.8'/><circle cx='17' cy='17.5' r='1.8'/>",

        // Movimientos de inventario
        ["entrada"] = "<path d='M12 3v12'/><path d='m7 10 5 5 5-5'/><path d='M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2'/>",
        ["salida"] = "<path d='M12 15V3'/><path d='m7 8 5-5 5 5'/><path d='M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2'/>",
        ["ajuste"] = "<path d='M21 6h-4M13 6H3'/><circle cx='15' cy='6' r='2'/><path d='M21 12h-9M8 12H3'/><circle cx='10' cy='12' r='2'/><path d='M21 18h-3M14 18H3'/><circle cx='16' cy='18' r='2'/>",
        ["historial"] = "<rect x='8' y='2.5' width='8' height='4' rx='1'/><path d='M16 4.5h2a2 2 0 0 1 2 2V20a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6.5a2 2 0 0 1 2-2h2'/><path d='M12 11h4M12 16h4M8 11h.01M8 16h.01'/>"
    };

    public static IHtmlContent Svg(string nombre) =>
        new HtmlString(
            "<svg class='icono' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' " +
            "stroke-linecap='round' stroke-linejoin='round' aria-hidden='true'>" +
            Trazos[nombre] + "</svg>");
}
