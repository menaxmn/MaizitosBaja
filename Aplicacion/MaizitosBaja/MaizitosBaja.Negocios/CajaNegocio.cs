using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Apertura y corte de caja (uno por día).
public class CajaNegocio
{
    private readonly CajaDatos _caja;

    public CajaNegocio(CajaDatos caja)
    {
        _caja = caja;
    }

    public CorteCaja? ObtenerAbierta() => _caja.ObtenerAbierta();

    public CorteCaja? ObtenerCorte(int corteId) => _caja.ObtenerPorId(corteId);

    public CorteCaja? ObtenerUltimoCerrado() => _caja.ObtenerUltimoCerrado();

    public Resultado Abrir(decimal fondoInicial)
    {
        if (fondoInicial < 0)
            return Resultado.Error("El fondo inicial no puede ser negativo.");

        try
        {
            int corteId = _caja.Abrir(Sesion.UsuarioId, fondoInicial);
            return Resultado.Ok($"Caja abierta con {fondoInicial:C} de fondo. ¡A vender! 🌽", corteId);
        }
        catch (MySqlException ex)
        {
            return Resultado.Error(ex.Message);
        }
    }

    public Resultado Cerrar(decimal efectivoContado, string? observaciones)
    {
        if (efectivoContado < 0)
            return Resultado.Error("El efectivo contado no puede ser negativo.");

        observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        if (observaciones?.Length > 200)
            return Resultado.Error("Las observaciones no pueden pasar de 200 caracteres.");

        var abierta = _caja.ObtenerAbierta();
        if (abierta == null)
            return Resultado.Error("No hay caja abierta para cerrar.");

        try
        {
            decimal diferencia = _caja.Cerrar(efectivoContado, observaciones);

            string texto = diferencia == 0 ? "La caja cuadró exacto."
                         : diferencia > 0 ? $"Sobran {diferencia:C}."
                         : $"Faltan {-diferencia:C}.";

            return Resultado.Ok("Caja cerrada. " + texto, abierta.CorteId);
        }
        catch (MySqlException ex)
        {
            return Resultado.Error(ex.Message);
        }
    }
}
