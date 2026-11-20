namespace MaizitosBaja.Entidades;

// Categoría del inventario (Paletas, Sodas, Vasos...).
// Tipo agrupa en: Comida, Bebida o Material.
public class Categoria
{
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public bool Activo { get; set; } = true;
}
