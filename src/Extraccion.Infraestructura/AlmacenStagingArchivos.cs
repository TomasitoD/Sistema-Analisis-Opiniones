using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Extraccion.Dominio;
using Microsoft.Extensions.Options;

namespace Extraccion.Infraestructura;

// Staging en archivos: una carpeta por corrida y un archivo .jsonl por fuente
// (un objeto JSON por linea).
public class AlmacenStagingArchivos : IAlmacenStaging
{
    private readonly string _raiz;

    public AlmacenStagingArchivos(IOptions<OpcionesExtraccion> opciones)
    {
        _raiz = Path.GetFullPath(opciones.Value.CarpetaStaging);
    }

    public string CrearCorrida()
    {
        var id = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var carpeta = Path.Combine(_raiz, id);
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    public IEscritorStaging AbrirEscritor(string carpeta, string fuente)
    {
        return new EscritorJsonLineas(Path.Combine(carpeta, fuente + ".jsonl"));
    }

    // Este archivo solo existe si la corrida termino bien
    public void Confirmar(string carpeta, ResultadoExtraccion resultado)
    {
        var opciones = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(resultado, opciones);
        File.WriteAllText(Path.Combine(carpeta, "resumen.json"), json, new UTF8Encoding(false));
    }

    public void Descartar(string carpeta)
    {
        if (Directory.Exists(carpeta))
            Directory.Delete(carpeta, true);
    }
}

public class EscritorJsonLineas : IEscritorStaging
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        // para que las tildes se guarden tal cual
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly StreamWriter _escritor;

    public EscritorJsonLineas(string ruta)
    {
        _escritor = new StreamWriter(ruta, false, new UTF8Encoding(false));
    }

    public async Task EscribirAsync(RegistroExtraido registro, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var linea = JsonSerializer.Serialize(registro.Campos, Opciones);
        await _escritor.WriteLineAsync(linea);
    }

    public async ValueTask DisposeAsync()
    {
        await _escritor.DisposeAsync();
    }
}
