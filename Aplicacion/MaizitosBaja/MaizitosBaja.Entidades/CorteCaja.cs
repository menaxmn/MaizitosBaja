namespace MaizitosBaja.Entidades;

// Corte de caja del día, con su resumen (sale de vw_ResumenCorte).
public class CorteCaja
{
    public int CorteId { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCorte { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string Estado { get; set; } = "";
    public decimal FondoInicial { get; set; }
    public decimal? EfectivoContado { get; set; }
    public string? Observaciones { get; set; }

    // Resumen
    public int NumVentas { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal VentasTarjeta { get; set; }
    public decimal VentasTransferencia { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal? Diferencia { get; set; }   // negativo = falta, positivo = sobra

    public bool EsDeHoy => FechaCorte.Date == DateTime.Today;
}
