using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Extraccion.Dominio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Extraccion.Infraestructura.Extractores;

// Consume los comentarios de redes sociales desde una API REST.
public class ApiExtractor : IExtractor
{
    public const string NombreCliente = "api-comentarios";

    private readonly IHttpClientFactory _fabrica;
    private readonly OpcionesApi _opciones;
    private readonly ILogger<ApiExtractor> _log;

    public string Nombre => "comentarios-api";

    public ApiExtractor(IHttpClientFactory fabrica, IOptions<OpcionesExtraccion> opciones, ILogger<ApiExtractor> log)
    {
        _fabrica = fabrica;
        _opciones = opciones.Value.Api;
        _log = log;
    }

    public async IAsyncEnumerable<RegistroExtraido> ExtraerAsync([EnumeratorCancellation] CancellationToken ct)
    {
        // Solo se permite HTTPS, o HTTP si la API esta en la misma maquina (localhost)
        var url = new Uri(_opciones.UrlBase);
        if (url.Scheme != Uri.UriSchemeHttps && !url.IsLoopback)
            throw new InvalidOperationException("La API debe usar HTTPS (HTTP solo se permite en localhost).");

        var cliente = _fabrica.CreateClient(NombreCliente);

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, _opciones.Ruta);
        if (!string.IsNullOrWhiteSpace(_opciones.Token))
            solicitud.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _opciones.Token);

        using var respuesta = await cliente.SendAsync(
            solicitud, HttpCompletionOption.ResponseHeadersRead, ct);
        respuesta.EnsureSuccessStatusCode();

        await using var contenido = await respuesta.Content.ReadAsStreamAsync(ct);

        // El JSON se lee objeto por objeto, no se carga completo en memoria
        var elementos = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
            contenido, cancellationToken: ct);

        await foreach (var elemento in elementos)
        {
            if (elemento.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException(
                    "La API devolvio algo que no es un objeto JSON.");

            var campos = new Dictionary<string, string?>();
            foreach (var propiedad in elemento.EnumerateObject())
            {
                campos[propiedad.Name] = propiedad.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => propiedad.Value.GetString(),
                    _ => propiedad.Value.GetRawText()
                };
            }

            yield return new RegistroExtraido(Nombre, campos);
        }
    }
}
