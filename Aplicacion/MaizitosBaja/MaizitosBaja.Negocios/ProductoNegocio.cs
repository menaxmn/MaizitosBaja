using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Reglas de los platillos del menú.
public class ProductoNegocio
{
    private readonly ProductoDatos _productos;

    public ProductoNegocio(ProductoDatos productos)
    {
        _productos = productos;
    }

    public List<Producto> Listar(string? buscar, bool incluirInactivos)
    {
        var lista = _productos.Listar(soloActivos: !incluirInactivos).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            string texto = buscar.Trim();
            lista = lista.Where(p =>
                p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (p.Descripcion ?? "").Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        return lista.ToList();
    }

    public Producto? Obtener(int id) => _productos.ObtenerPorId(id);

    public Resultado Agregar(Producto p)
    {
        Normalizar(p);
        string? error = Validar(p);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            int id = _productos.Insertar(p);
            return Resultado.Ok($"Se agregó \"{p.Nombre}\" al menú.", id);
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    public Resultado Actualizar(Producto p)
    {
        Normalizar(p);

        if (_productos.ObtenerPorId(p.ProductoId) == null)
            return Resultado.Error("El platillo ya no existe.");

        string? error = Validar(p);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            _productos.Actualizar(p);
            return Resultado.Ok($"Se guardaron los cambios de \"{p.Nombre}\".");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    // Activo <-> inactivo (platillos de temporada). Nunca se borra,
    // porque las ventas pasadas lo siguen usando.
    public Resultado CambiarEstado(int id)
    {
        var p = _productos.ObtenerPorId(id);
        if (p == null)
            return Resultado.Error("El platillo ya no existe.");

        try
        {
            _productos.CambiarActivo(id, !p.Activo);
            return Resultado.Ok(p.Activo
                ? $"\"{p.Nombre}\" se quitó del menú (ya no aparece en Vender)."
                : $"\"{p.Nombre}\" volvió al menú.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo cambiar el estado: " + ex.Message);
        }
    }

    private static void Normalizar(Producto p)
    {
        // Quita espacios dobles para que "Vaso  de elote chico" no rompa la agrupación por tamaño
        p.Nombre = string.Join(' ', (p.Nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        p.Descripcion = string.IsNullOrWhiteSpace(p.Descripcion) ? null : p.Descripcion.Trim();
    }

    private string? Validar(Producto p)
    {
        if (p.Nombre.Length == 0)
            return "Escribe el nombre del platillo.";
        if (p.Nombre.Length > 80)
            return "El nombre no puede pasar de 80 caracteres.";
        if (p.Descripcion?.Length > 250)
            return "La descripción no puede pasar de 250 caracteres.";
        if (p.Precio <= 0)
            return "El precio debe ser mayor a 0.";
        if (p.Precio > 99999)
            return "Revisa el precio, es demasiado alto.";
        if (_productos.ExisteNombre(p.Nombre, p.ProductoId))
            return $"Ya existe un platillo llamado \"{p.Nombre}\".";
        return null;
    }
}
