namespace MaizitosBaja.Entidades;

// Platillo del menú (Tostilocos, Vaso de elote, Buldak...).
public class Producto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
}
