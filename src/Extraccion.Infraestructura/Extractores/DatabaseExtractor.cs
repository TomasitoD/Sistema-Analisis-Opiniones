using System.Globalization;
using System.Runtime.CompilerServices;
using Extraccion.Dominio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Extraccion.Infraestructura.Extractores;

// Lee las resenas web desde la base de datos transaccional (MySQL).
public class DatabaseExtractor : IExtractor
{
    private readonly OpcionesBaseDatos _opciones;
    private readonly ILogger<DatabaseExtractor> _log;

    public string Nombre => "resenas-bd";

    // Solo se lee de la vista, nunca de las tablas directamente
    private const string Consulta = @"
        SELECT IdExternoOpinion, IdExternoCliente, Cliente,
               IdExternoProducto, Producto, Categoria,
               Fecha, Comentario, PuntajeSatisfaccion
        FROM vw_OpinionesDetalle
        WHERE Canal = @canal
        ORDER BY IdOpinion";

    public DatabaseExtractor(IOptions<OpcionesExtraccion> opciones, ILogger<DatabaseExtractor> log)
    {
        _opciones = opciones.Value.BaseDatos;
        _log = log;
    }

    public async IAsyncEnumerable<RegistroExtraido> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opciones.CadenaConexion))
            throw new InvalidOperationException("Falta la cadena de conexion. Definala en la variable de entorno Extraccion__BaseDatos__CadenaConexion.");

        await using var conexion = new MySqlConnection(_opciones.CadenaConexion);
        await conexion.OpenAsync(ct);

        await using var comando = conexion.CreateCommand();
        comando.CommandText = Consulta;
        comando.Parameters.AddWithValue("@canal", _opciones.Canal);

        await using var lector = await comando.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct))
        {
            var campos = new Dictionary<string, string?>();
            for (int i = 0; i < lector.FieldCount; i++)
                campos[lector.GetName(i)] =
                    lector.IsDBNull(i) ? null : ATexto(lector.GetValue(i));

            yield return new RegistroExtraido(Nombre, campos);
        }
    }

    private static string ATexto(object valor)
    {
        if (valor is DateTime fecha)
            return fecha.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return Convert.ToString(valor, CultureInfo.InvariantCulture) ?? "";
    }
}
