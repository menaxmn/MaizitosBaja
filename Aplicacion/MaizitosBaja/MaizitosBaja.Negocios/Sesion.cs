namespace MaizitosBaja.Negocios;

// El negocio usa una sola cuenta (administrador), por petición de la dueña.
// Es el usuario 'admin' que se carga en 02_datos.sql (UsuarioId = 1).
public static class Sesion
{
    public const int UsuarioId = 1;
}
