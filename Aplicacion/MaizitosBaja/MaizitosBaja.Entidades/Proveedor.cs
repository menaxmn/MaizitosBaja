namespace MaizitosBaja.Entidades;

// A quién le compra el negocio.
public class Proveedor
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public bool Activo { get; set; } = true;
}
