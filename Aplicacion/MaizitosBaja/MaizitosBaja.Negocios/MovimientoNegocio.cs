using MaizitosBaja.Datos;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Negocios;

// Entradas, salidas (uso / merma) y ajustes de inventario.
// El stock nunca se cambia directo: se registra un movimiento y
// los triggers lo validan y actualizan.
public class MovimientoNegocio
{
    private readonly MovimientoDatos _movimientos;
    private readonly ArticuloDatos _articulos;
    private readonly ProveedorDatos _proveedores;

    public MovimientoNegocio(MovimientoDatos movimientos, ArticuloDatos articulos, ProveedorDatos proveedores)
    {
        _movimientos = movimientos;
        _articulos = articulos;
        _proveedores = proveedores;
    }

    // ---------- Consultas ----------

    public List<Articulo> ListarArticulosActivos() =>
        _articulos.Listar().Where(a => a.Activo).ToList();

    // Para el filtro del historial (incluye desactivados, que también tienen movimientos)
    public List<Articulo> ListarTodosLosArticulos() => _articulos.Listar();

    public List<Proveedor> ListarProveedoresActivos() => _proveedores.Listar(soloActivos: true);

    public List<MovimientoInventario> Historial(DateTime? desde, DateTime? hasta, string? tipo, int? articuloId) =>
        _movimientos.Listar(desde, hasta, string.IsNullOrWhiteSpace(tipo) ? null : tipo, articuloId);

    // ---------- Entrada (compra) ----------

    public Resultado RegistrarEntrada(List<RenglonEntrada> renglones, int? proveedorId, string? observaciones)
    {
        observaciones = Limpiar(observaciones);
        if (observaciones?.Length > 200)
            return Resultado.Error("Las observaciones no pueden pasar de 200 caracteres.");

        // Si el mismo artículo viene dos veces, se suman
        var lista = (renglones ?? new())
            .Where(r => r.ArticuloId > 0)
            .GroupBy(r => r.ArticuloId)
            .Select(g => new RenglonEntrada { ArticuloId = g.Key, Cantidad = g.Sum(r => r.Cantidad) })
            .ToList();

        if (lista.Count == 0)
            return Resultado.Error("Agrega al menos un artículo a la entrada.");
        if (lista.Any(r => r.Cantidad <= 0 || r.Cantidad > 100000))
            return Resultado.Error("Las cantidades deben ser mayores a 0.");

        var activos = ListarArticulosActivos().Select(a => a.ArticuloId).ToHashSet();
        if (lista.Any(r => !activos.Contains(r.ArticuloId)))
            return Resultado.Error("Uno de los artículos no existe o está desactivado.");

        if (proveedorId.HasValue && !ListarProveedoresActivos().Any(p => p.ProveedorId == proveedorId))
            return Resultado.Error("El proveedor seleccionado no existe o está desactivado.");

        try
        {
            _movimientos.RegistrarEntrada(lista, proveedorId, observaciones, Sesion.UsuarioId);
            int piezas = lista.Sum(r => r.Cantidad);
            return Resultado.Ok($"Entrada registrada: {lista.Count} artículo(s), {piezas} pieza(s)/paquete(s) en total.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error("No se guardó la entrada: " + ex.Message);
        }
    }

    // ---------- Salida (uso o merma) ----------

    public Resultado RegistrarSalida(int articuloId, string? tipo, int cantidad, string? motivo)
    {
        motivo = Limpiar(motivo);

        if (tipo != "Uso" && tipo != "Merma")
            return Resultado.Error("Elige si es uso o merma.");
        if (cantidad <= 0)
            return Resultado.Error("La cantidad debe ser mayor a 0.");
        if (tipo == "Merma" && motivo == null)
            return Resultado.Error("Escribe el motivo de la merma (ej. se derritieron, se caducó).");
        if (motivo?.Length > 200)
            return Resultado.Error("El motivo no puede pasar de 200 caracteres.");

        var a = _articulos.ObtenerPorId(articuloId);
        if (a == null || !a.Activo)
            return Resultado.Error("Selecciona un artículo activo.");

        try
        {
            // Si no alcanza, el trigger responde "Stock insuficiente de ..."
            _movimientos.Registrar(articuloId, tipo, -cantidad, motivo, Sesion.UsuarioId);
            return Resultado.Ok($"{(tipo == "Uso" ? "Uso" : "Merma")} registrada: salieron {cantidad} de {a.NombreCompleto}. Quedan {a.Stock - cantidad}.");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error(ex.Message);
        }
    }

    // ---------- Ajuste por conteo físico ----------

    public Resultado RegistrarAjuste(int articuloId, int conteoReal, string? motivo)
    {
        motivo = Limpiar(motivo);

        if (conteoReal < 0)
            return Resultado.Error("El conteo no puede ser negativo.");
        if (motivo == null)
            return Resultado.Error("Escribe el motivo del ajuste (ej. conteo semanal).");
        if (motivo.Length > 200)
            return Resultado.Error("El motivo no puede pasar de 200 caracteres.");

        var a = _articulos.ObtenerPorId(articuloId);
        if (a == null || !a.Activo)
            return Resultado.Error("Selecciona un artículo activo.");

        int diferencia = conteoReal - a.Stock;
        if (diferencia == 0)
            return Resultado.Ok($"El conteo de {a.NombreCompleto} coincide con el sistema ({a.Stock}). No se registró ajuste.");

        try
        {
            _movimientos.Registrar(articuloId, "Ajuste", diferencia, motivo, Sesion.UsuarioId);
            return Resultado.Ok($"Ajuste registrado: {a.NombreCompleto} pasó de {a.Stock} a {conteoReal} ({diferencia:+#;-#}).");
        }
        catch (MySqlException ex)
        {
            return Resultado.Error(ex.Message);
        }
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
