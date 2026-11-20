using System.Text.RegularExpressions;
using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Catálogo de proveedores (quién les lleva las sodas, botanas, desechables...).
public class ProveedorNegocio
{
    private readonly ProveedorDatos _proveedores;

    public ProveedorNegocio(ProveedorDatos proveedores)
    {
        _proveedores = proveedores;
    }

    public List<Proveedor> Listar(string? buscar, bool incluirInactivos)
    {
        var lista = _proveedores.Listar(soloActivos: !incluirInactivos).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            string texto = buscar.Trim();
            lista = lista.Where(p =>
                p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (p.Telefono ?? "").Contains(texto) ||
                (p.Correo ?? "").Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        return lista.ToList();
    }

    public Proveedor? Obtener(int id) => _proveedores.ObtenerPorId(id);

    public Resultado Agregar(Proveedor p)
    {
        Normalizar(p);
        string? error = Validar(p);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            int id = _proveedores.Insertar(p);
            return Resultado.Ok($"Se agregó el proveedor \"{p.Nombre}\".", id);
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    public Resultado Actualizar(Proveedor p)
    {
        Normalizar(p);

        if (_proveedores.ObtenerPorId(p.ProveedorId) == null)
            return Resultado.Error("El proveedor ya no existe.");

        string? error = Validar(p);
        if (error != null)
            return Resultado.Error(error);

        try
        {
            _proveedores.Actualizar(p);
            return Resultado.Ok($"Se guardaron los cambios de \"{p.Nombre}\".");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo guardar: " + ex.Message);
        }
    }

    public Resultado CambiarEstado(int id)
    {
        var p = _proveedores.ObtenerPorId(id);
        if (p == null)
            return Resultado.Error("El proveedor ya no existe.");

        try
        {
            _proveedores.CambiarActivo(id, !p.Activo);
            return Resultado.Ok(p.Activo
                ? $"\"{p.Nombre}\" se desactivó (ya no aparece al registrar entradas)."
                : $"\"{p.Nombre}\" se activó de nuevo.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se pudo cambiar el estado: " + ex.Message);
        }
    }

    private static void Normalizar(Proveedor p)
    {
        p.Nombre = (p.Nombre ?? "").Trim();
        p.Telefono = string.IsNullOrWhiteSpace(p.Telefono) ? null : p.Telefono.Trim();
        p.Correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim();
    }

    private string? Validar(Proveedor p)
    {
        if (p.Nombre.Length == 0)
            return "Escribe el nombre del proveedor.";
        if (p.Nombre.Length > 100)
            return "El nombre no puede pasar de 100 caracteres.";
        if (p.Telefono != null && !Regex.IsMatch(p.Telefono, @"^[0-9 +()\-]{7,15}$"))
            return "El teléfono solo puede llevar números, espacios o guiones (7 a 15 caracteres).";
        if (p.Correo != null && (p.Correo.Length > 100 || !Regex.IsMatch(p.Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
            return "Revisa el correo, no parece válido.";
        if (_proveedores.ExisteNombre(p.Nombre, p.ProveedorId))
            return $"Ya existe un proveedor llamado \"{p.Nombre}\".";
        return null;
    }
}
