using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MaizitosBaja.Web.Pages.Sistema;

// Apaga el sistema instalado (el servidor que corre en segundo plano).
public class CerrarModel : PageModel
{
    private readonly IHostApplicationLifetime _vida;
    private readonly IWebHostEnvironment _entorno;

    public bool Cerrado { get; private set; }

    public CerrarModel(IHostApplicationLifetime vida, IWebHostEnvironment entorno)
    {
        _vida = vida;
        _entorno = entorno;
    }

    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost()
    {
        // En Visual Studio no se apaga desde aquí
        if (_entorno.IsDevelopment())
            return RedirectToPage("/Index");

        Cerrado = true;

        // Se apaga un momento después, para que alcance a llegar esta página
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            _vida.StopApplication();
        });

        return Page();
    }
}
