using System.Globalization;
using MaizitosBaja.Datos;
using MaizitosBaja.Negocios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Conexión a MySQL (la cadena viene de User Secrets)
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
