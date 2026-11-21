using System.Diagnostics;
using Extraccion.Dominio;
using Microsoft.Extensions.Logging;

namespace Extraccion.Aplicacion;

// Coordina la extraccion: lanza un extractor por fuente en paralelo
// y guarda lo que sale de cada uno en el staging.
public class ProcesoExtraccion
{
    private readonly List<IExtractor> _extractores;
    private readonly IAlmacenStaging _staging;
    private readonly ILogger<ProcesoExtraccion> _log;

    public ProcesoExtraccion(IEnumerable<IExtractor> extractores, IAlmacenStaging staging, ILogger<ProcesoExtraccion> log)
    {
        _extractores = extractores.ToList();
        _staging = staging;
        _log = log;
    }

    public async Task<ResultadoExtraccion> EjecutarAsync(CancellationToken ct)
    {
        if (_extractores.Count == 0)
            throw new InvalidOperationException("No hay ninguna fuente habilitada en appsettings.json.");

        var reloj = Stopwatch.StartNew();
        var carpeta = _staging.CrearCorrida();
        _log.LogInformation("Inicia la corrida con {Cantidad} fuentes. Carpeta: {Carpeta}", _extractores.Count, carpeta);

        // Si una fuente falla se cancelan las demas
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var tareas = _extractores.Select(e => ExtraerFuenteAsync(e, carpeta, cts)).ToList();

        try
        {
            var fuentes = (await Task.WhenAll(tareas)).ToList();
            reloj.Stop();

            var resultado = new ResultadoExtraccion(
                carpeta, fuentes, reloj.ElapsedMilliseconds);
            _staging.Confirmar(carpeta, resultado);

            _log.LogInformation("Corrida terminada. Filas={Filas} DuracionMs={Ms}",
                resultado.TotalFilas, resultado.DuracionTotalMs);
            return resultado;
        }
        catch (Exception)
        {
            cts.Cancel();
            // esperar a que todas las tareas terminen antes de borrar la carpeta
            await Task.WhenAll(tareas.Select(t => t.ContinueWith(_ => { })));
            _staging.Descartar(carpeta);
            _log.LogWarning("La corrida fallo y se borro el staging parcial.");

            // Si hubo un error real, se devuelve ese y no la cancelacion de las otras tareas
            var fallo = tareas.FirstOrDefault(t => t.IsFaulted)?.Exception?.InnerException;
            if (fallo != null)
                throw fallo;
            throw;
        }
    }

    private async Task<ResultadoFuente> ExtraerFuenteAsync(IExtractor extractor, string carpeta, CancellationTokenSource cts)
    {
        var reloj = Stopwatch.StartNew();
        try
        {
            int filas = 0;
            await using var escritor = _staging.AbrirEscritor(carpeta, extractor.Nombre);

            await foreach (var registro in extractor.ExtraerAsync(cts.Token))
            {
                await escritor.EscribirAsync(registro, cts.Token);
                filas++;
            }

            reloj.Stop();
            _log.LogInformation("Fuente {Fuente} lista. Filas={Filas} DuracionMs={Ms}", extractor.Nombre, filas, reloj.ElapsedMilliseconds);
            return new ResultadoFuente(extractor.Nombre, filas, reloj.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Solo se registra el primer error; los demas son cancelaciones por consecuencia
            if (!cts.IsCancellationRequested)
                _log.LogError("Fallo la fuente {Fuente}: {Tipo}: {Mensaje}",
                    extractor.Nombre, ex.GetType().Name, ex.Message);
            cts.Cancel();
            throw;
        }
    }
}
