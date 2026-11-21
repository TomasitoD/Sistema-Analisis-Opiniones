namespace Extraccion.Dominio;

// Cada fuente de datos (CSV, BD, API) implementa esta interfaz.
public interface IExtractor
{
    string Nombre { get; }
    IAsyncEnumerable<RegistroExtraido> ExtraerAsync(CancellationToken ct);
}

// Escribe los registros de una fuente en el staging.
public interface IEscritorStaging : IAsyncDisposable
{
    Task EscribirAsync(RegistroExtraido registro, CancellationToken ct);
}

// Maneja la carpeta de staging de cada corrida.
public interface IAlmacenStaging
{
    string CrearCorrida();
    IEscritorStaging AbrirEscritor(string carpeta, string fuente);
    void Confirmar(string carpeta, ResultadoExtraccion resultado);
    void Descartar(string carpeta);
}
