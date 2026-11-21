namespace Extraccion.Dominio;

// Un registro tal como viene de la fuente (sin transformar).
public record RegistroExtraido(string Fuente, Dictionary<string, string?> Campos);

// Resultado de extraer una sola fuente.
public record ResultadoFuente(string Fuente, int Filas, long DuracionMs);

// Resultado de toda la corrida.
public record ResultadoExtraccion(string Carpeta, List<ResultadoFuente> Fuentes, long DuracionTotalMs)
{
    public int TotalFilas => Fuentes.Sum(f => f.Filas);
}
