using System.Data;
using MaizitosBaja.Entidades;
using MySqlConnector;

namespace MaizitosBaja.Datos;

// Apertura y cierre de caja. Usa sp_AbrirCorte, sp_CerrarCorte
// y la vista vw_ResumenCorte.
public class CajaDatos
{
    private readonly ConexionBD _conexion;

    private const string SelectResumen = @"
        SELECT r.CorteId, r.FechaCorte, r.Estado, r.FondoInicial, r.NumVentas,
               r.VentasEfectivo, r.VentasTarjeta, r.VentasTransferencia,
               r.TotalVendido, r.EfectivoEsperado, r.EfectivoContado, r.Diferencia,
               c.FechaApertura, c.FechaCierre, c.Observaciones
          FROM vw_ResumenCorte r
          JOIN CorteCaja c ON c.CorteId = r.CorteId";

    public CajaDatos(ConexionBD conexion)
    {
        _conexion = conexion;
    }

    // La caja abierta, o null si no hay
    public CorteCaja? ObtenerAbierta()
    {
        return LeerUno(SelectResumen + " WHERE r.Estado = 'Abierto' ORDER BY r.CorteId DESC LIMIT 1", null);
    }

    // El corte cerrado más reciente (para la pantalla de Inicio)
    public CorteCaja? ObtenerUltimoCerrado()
    {
        return LeerUno(SelectResumen + " WHERE r.Estado = 'Cerrado' ORDER BY r.CorteId DESC LIMIT 1", null);
    }

    public CorteCaja? ObtenerPorId(int corteId)
    {
        return LeerUno(SelectResumen + " WHERE r.CorteId = @id", corteId);
    }

    public int Abrir(int usuarioId, decimal fondoInicial)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand("sp_AbrirCorte", cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@p_UsuarioId", usuarioId);
        cmd.Parameters.AddWithValue("@p_FondoInicial", fondoInicial);
        var salida = new MySqlParameter("@p_CorteId", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(salida);

        cmd.ExecuteNonQuery();
        return Convert.ToInt32(salida.Value);
    }

    // Devuelve la diferencia (negativo = falta, positivo = sobra)
    public decimal Cerrar(decimal efectivoContado, string? observaciones)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand("sp_CerrarCorte", cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@p_EfectivoContado", efectivoContado);
        cmd.Parameters.AddWithValue("@p_Observaciones", (object?)observaciones ?? DBNull.Value);
        var salida = new MySqlParameter("@p_Diferencia", MySqlDbType.Decimal) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(salida);

        // El procedimiento también regresa el resumen (SELECT); aquí no se usa,
        // pero hay que leerlo completo para que llegue el parámetro de salida.
        using (var r = cmd.ExecuteReader())
        {
            do { while (r.Read()) { } } while (r.NextResult());
        }

        return salida.Value == DBNull.Value ? 0 : Convert.ToDecimal(salida.Value);
    }

    private CorteCaja? LeerUno(string sql, int? id)
    {
        using var cn = _conexion.CrearConexion();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        if (id.HasValue)
            cmd.Parameters.AddWithValue("@id", id.Value);

        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;

        return new CorteCaja
        {
            CorteId             = Convert.ToInt32(r["CorteId"]),
            FechaCorte          = Convert.ToDateTime(r["FechaCorte"]),
            Estado              = Convert.ToString(r["Estado"]) ?? "",
            FondoInicial        = Convert.ToDecimal(r["FondoInicial"]),
            NumVentas           = Convert.ToInt32(r["NumVentas"]),
            VentasEfectivo      = Convert.ToDecimal(r["VentasEfectivo"]),
            VentasTarjeta       = Convert.ToDecimal(r["VentasTarjeta"]),
            VentasTransferencia = Convert.ToDecimal(r["VentasTransferencia"]),
            TotalVendido        = Convert.ToDecimal(r["TotalVendido"]),
            EfectivoEsperado    = Convert.ToDecimal(r["EfectivoEsperado"]),
            EfectivoContado     = r["EfectivoContado"] == DBNull.Value ? null : Convert.ToDecimal(r["EfectivoContado"]),
            Diferencia          = r["Diferencia"] == DBNull.Value ? null : Convert.ToDecimal(r["Diferencia"]),
            FechaApertura       = Convert.ToDateTime(r["FechaApertura"]),
            FechaCierre         = r["FechaCierre"] == DBNull.Value ? null : Convert.ToDateTime(r["FechaCierre"]),
            Observaciones       = r["Observaciones"] == DBNull.Value ? null : Convert.ToString(r["Observaciones"])
        };
    }
}
