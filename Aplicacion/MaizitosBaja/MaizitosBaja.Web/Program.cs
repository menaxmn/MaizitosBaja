using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using MaizitosBaja.Datos;
using MaizitosBaja.Negocios;

// ---------------------------------------------------------------------------
// Dos formas de correr el sistema:
//  - Desarrollo (Visual Studio): igual que siempre, https y User Secrets.
//  - Instalado (C:\MaizitosBaja\App): http://localhost:5050, la conexión viene
//    de appsettings.Production.json y se abre solo en una ventana tipo programa.
// ---------------------------------------------------------------------------
bool esDesarrollo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
const int PuertoInstalado = 5050;
string urlInstalado = $"http://localhost:{PuertoInstalado}";

if (!esDesarrollo && PuertoOcupado(PuertoInstalado))
{
    // Ya está abierto: no se arranca otro, solo se muestra la ventana
    AbrirVentana(urlInstalado);
    return;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Instalado: los archivos (wwwroot, appsettings) están junto al .exe,
    // aunque se abra desde un acceso directo en el escritorio
    ContentRootPath = esDesarrollo ? null : AppContext.BaseDirectory
});

if (!esDesarrollo)
    builder.WebHost.UseUrls(urlInstalado);   // solo esta computadora (localhost)

builder.Services.AddRazorPages();

// Conexión a MySQL:
//  - Desarrollo: User Secrets
//  - Instalado: appsettings.Production.json junto al .exe
var cadena = builder.Configuration.GetConnectionString("MaizitosBaja") ?? "";
builder.Services.AddSingleton(new ConexionBD(cadena));

// Capa de Datos y de Negocios
builder.Services.AddScoped<CategoriaDatos>();
builder.Services.AddScoped<ProveedorDatos>();
builder.Services.AddScoped<ArticuloDatos>();
builder.Services.AddScoped<ProductoDatos>();
builder.Services.AddScoped<VentaDatos>();
builder.Services.AddScoped<CajaDatos>();
builder.Services.AddScoped<MovimientoDatos>();
builder.Services.AddScoped<ArticuloNegocio>();
builder.Services.AddScoped<ProductoNegocio>();
builder.Services.AddScoped<VentaNegocio>();
builder.Services.AddScoped<CajaNegocio>();
builder.Services.AddScoped<ProveedorNegocio>();
builder.Services.AddScoped<MovimientoNegocio>();

// Datos del negocio para el ticket (sección "Negocio" de appsettings.json)
builder.Services.AddSingleton(
    builder.Configuration.GetSection("Negocio").Get<MaizitosBaja.Web.DatosNegocio>()
    ?? new MaizitosBaja.Web.DatosNegocio());

// Pesos mexicanos y punto decimal en toda la app
var cultura = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    // Instalado: sin https (solo se usa en esta computadora) y con página de error amigable
    app.UseExceptionHandler("/Error");

    // Al terminar de arrancar, se abre la ventana del sistema
    app.Lifetime.ApplicationStarted.Register(() => AbrirVentana(urlInstalado));
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();


// ---------- Funciones de apoyo ----------

// ¿Ya hay algo escuchando en ese puerto? (o sea, el sistema ya está abierto)
static bool PuertoOcupado(int puerto)
{
    try
    {
        var prueba = new TcpListener(IPAddress.Loopback, puerto);
        prueba.Start();
        prueba.Stop();
        return false;
    }
    catch (SocketException)
    {
        return true;
    }
}

// Abre el sistema en una ventana de Edge sin barra de direcciones (parece programa).
// Si Edge no está, usa el navegador predeterminado.
static void AbrirVentana(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo("msedge", $"--app={url} --start-maximized") { UseShellExecute = true });
    }
    catch
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}
