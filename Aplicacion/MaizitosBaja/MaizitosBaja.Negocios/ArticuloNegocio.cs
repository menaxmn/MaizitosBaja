using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Reglas del inventario. La página web le pide todo a esta clase
// y esta clase valida antes de llamar a la capa de Datos.
public class ArticuloNegocio
{
    private static readonly string[] Unidades = { "Pieza", "Paquete" };

    private readonly ArticuloDatos _articulos;
    private readonly CategoriaDatos _categorias;
    private readonly ProveedorDatos _proveedores;

    public ArticuloNegocio(ArticuloDatos articulos, CategoriaDatos categorias, ProveedorDatos proveedores)
    {
        _articulos = articulos;
        _categorias = categorias;
        _proveedores = proveedores;
    }

    // ---------- Consultas ----------

    // tipo: Comida, Bebida, Material o null (todos)
    public List<Articulo> Listar(string? tipo, string? buscar, bool incluirInactivos)
    {
        var lista = _articulos.Listar().AsEnumerable();

        if (!incluirInactivos)
            lista = lista.Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(tipo))
            lista = lista.Where(a => a.Tipo == tipo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            string texto = buscar.Trim();
            lista = lista.Where(a =>
                a.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                a.Categoria.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        return lista.ToList();
    }

    public Articulo? Obtener(int id) => _articulos.ObtenerPorId(id);

    public List<Categoria> ListarCategorias() => _categorias.Listar(soloActivas: true);

    public List<Proveedor> ListarProveedores() => _proveedores.Listar(soloActivos: true);

    // ---------- Operaciones ----------

    public Resultado Agregar(Articulo a, int stockInicial)
    {
        Normalizar(a);

        if (stockInicial < 0)
            return Resultado.Error("El stock inicial no puede ser negativo.");

        string? error = Validar(a);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            _articulos.Insertar(a, stockInicial, Sesion.UsuarioId);
            return Resultado.Ok($"Se agregó \"{a.NombreCompleto}\" al inventario.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    public Resultado Actualizar(Articulo a)
    {
        Normalizar(a);

        if (_articulos.ObtenerPorId(a.ArticuloId) == null)
            return Resultado.Error("El artículo ya no existe.");

        string? error = Validar(a);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            _articulos.Actualizar(a);
            return Resultado.Ok($"Se guardaron los cambios de \"{a.NombreCompleto}\".");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    // Si está activo lo desactiva y al revés
    public Resultado CambiarEstado(int id)
    {
        var a = _articulos.ObtenerPorId(id);
        if (a == null)
            return Resultado.Error("El artículo ya no existe.");

        try
        {
            _articulos.CambiarActivo(id, !a.Activo);
            return Resultado.Ok(a.Activo
                ? $"\"{a.NombreCompleto}\" se desactivó (ya no aparecerá para vender)."
                : $"\"{a.NombreCompleto}\" se activó de nuevo.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo cambiar el estado: " + ex.Message);
        }
    }

    // ---------- Reglas ----------

    // Limpia espacios y deja los valores como los espera la base
    private static void Normalizar(Articulo a)
    {
        a.Nombre = (a.Nombre ?? "").Trim();
        a.Variante = (a.Variante ?? "").Trim();

        // Si no se vende suelto, la base exige que el precio sea NULL
        if (!a.SeVendeSuelto)
            a.PrecioSuelto = null;
    }

    // Devuelve el primer error encontrado, o null si todo está bien
    private string? Validar(Articulo a)
    {
        if (a.Nombre.Length == 0)
            return "Escribe el nombre del artículo.";
        if (a.Nombre.Length > 60)
            return "El nombre no puede pasar de 60 caracteres.";
        if (a.Variante.Length > 40)
            return "La variante no puede pasar de 40 caracteres.";

        if (!_categorias.Listar(soloActivas: false).Any(c => c.CategoriaId == a.CategoriaId))
            return "Selecciona una categoría válida.";

        if (a.ProveedorId.HasValue &&
            !_proveedores.Listar(soloActivos: false).Any(p => p.ProveedorId == a.ProveedorId))
            return "El proveedor seleccionado no existe.";

        if (!Unidades.Contains(a.Unidad))
            return "La unidad debe ser Pieza o Paquete.";

        if (a.StockMinimo < 0)
            return "El stock mínimo no puede ser negativo.";

        if (a.SeVendeSuelto && (a.PrecioSuelto == null || a.PrecioSuelto <= 0))
            return "Si se vende suelto, escribe un precio mayor a 0.";

        if (_articulos.ExisteDuplicado(a.CategoriaId, a.Nombre, a.Variante, a.ArticuloId))
            return $"Ya existe \"{a.NombreCompleto}\" en esa categoría.";

        return null;
    }
}
