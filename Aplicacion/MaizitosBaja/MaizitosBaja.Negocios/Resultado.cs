namespace MaizitosBaja.Negocios;

// Respuesta de una operación de negocio: si salió bien y qué mensaje mostrar.
// Id: el registro que se creó (por ejemplo el número de venta), si aplica.
public class Resultado
{
    public bool Exito { get; private set; }
    public string Mensaje { get; private set; } = "";
    public int? Id { get; private set; }

    public static Resultado Ok(string mensaje, int? id = null) => new() { Exito = true, Mensaje = mensaje, Id = id };
    public static Resultado Error(string mensaje) => new() { Exito = false, Mensaje = mensaje };
}
