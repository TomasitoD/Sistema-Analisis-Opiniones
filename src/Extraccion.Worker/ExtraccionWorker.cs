using Extraccion.Aplicacion;

namespace Extraccion.Worker;

// Servicio que ejecuta la extraccion una vez y cierra la aplicacion.
// Codigos de salida: 0 = bien, 1 = fallo, 130 = cancelado.
public class ExtraccionWorker : BackgroundService
{
    private readonly ProcesoExtraccion _proceso;
    private readonly IHostApplicationLifetime _vida;
    private readonly ILogger<ExtraccionWorker> _log;

    public ExtraccionWorker(ProcesoExtraccion proceso, IHostApplicationLifetime vida, ILogger<ExtraccionWorker> log)
    {
        _proceso = proceso;
        _vida = vida;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _log.LogInformation("Worker de extraccion iniciado.");
            var resultado = await _proceso.EjecutarAsync(stoppingToken);
            _log.LogInformation("Extraccion completa: {Filas} filas en {Ms} ms.", resultado.TotalFilas, resultado.DuracionTotalMs);
            Environment.ExitCode = 0;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _log.LogWarning("Extraccion cancelada.");
            Environment.ExitCode = 130;
        }
        catch (Exception ex)
        {
            _log.LogError("La extraccion fallo: {Tipo}", ex.GetType().Name);
            Environment.ExitCode = 1;
        }
        finally
        {
            _vida.StopApplication();
        }
    }
}
