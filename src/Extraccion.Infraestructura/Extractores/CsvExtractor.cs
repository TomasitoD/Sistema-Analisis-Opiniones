using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Extraccion.Dominio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Extraccion.Infraestructura.Extractores;

// Lee las encuestas internas desde un archivo CSV.
public class CsvExtractor : IExtractor
{
    private readonly OpcionesCsv _opciones;
    private readonly ILogger<CsvExtractor> _log;

    public string Nombre => "encuestas-csv";

    public CsvExtractor(IOptions<OpcionesExtraccion> opciones, ILogger<CsvExtractor> log)
    {
        _opciones = opciones.Value.Csv;
        _log = log;
    }

    public async IAsyncEnumerable<RegistroExtraido> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var ruta = Path.GetFullPath(_opciones.Ruta);
        if (!File.Exists(ruta))
            throw new FileNotFoundException("No se encontro el archivo CSV de encuestas.", ruta);

        using var lector = new StreamReader(ruta, Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var csv = new CsvReader(lector, config);

        if (!await csv.ReadAsync())
            throw new InvalidDataException("El archivo CSV esta vacio.");

        csv.ReadHeader();
        var encabezados = csv.HeaderRecord ?? Array.Empty<string>();

        // Validar que esten los encabezados que se necesitan
        var faltantes = _opciones.EncabezadosRequeridos
            .Where(h => !encabezados.Contains(h))
            .ToList();
        if (faltantes.Count > 0)
            throw new InvalidDataException(
                "Faltan encabezados en el CSV: " + string.Join(", ", faltantes));

        int fila = 1;
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            fila++;

            var valores = csv.Parser.Record ?? Array.Empty<string>();
            if (valores.Length != encabezados.Length)
                throw new InvalidDataException(
                    $"La fila {fila} tiene {valores.Length} columnas y se esperaban {encabezados.Length}.");

            var campos = new Dictionary<string, string?>();
            for (int i = 0; i < encabezados.Length; i++)
                campos[encabezados[i]] = valores[i];

            yield return new RegistroExtraido(Nombre, campos);
        }
    }
}
