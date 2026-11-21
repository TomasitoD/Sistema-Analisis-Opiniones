using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using CsvHelper;

// API de demostracion: publica por HTTP los comentarios de redes sociales
// que estan en data/social_comments.csv. Se ejecuta desde la raiz del proyecto.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5080");
var app = builder.Build();

var ruta = app.Configuration["RutaCsv"] ?? "data/social_comments.csv";

var opcionesJson = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

app.MapGet("/api/comentarios", () =>
{
    if (!File.Exists(ruta))
        return Results.NotFound("No se encontro " + ruta);

    var lista = new List<Dictionary<string, string>>();

    using var lector = new StreamReader(ruta);
    using var csv = new CsvReader(lector, CultureInfo.InvariantCulture);
    csv.Read();
    csv.ReadHeader();
    var encabezados = csv.HeaderRecord ?? Array.Empty<string>();

    while (csv.Read())
    {
        var valores = csv.Parser.Record ?? Array.Empty<string>();
        var fila = new Dictionary<string, string>();
        for (int i = 0; i < encabezados.Length && i < valores.Length; i++)
            fila[encabezados[i]] = valores[i];
        lista.Add(fila);
    }

    return Results.Json(lista, opcionesJson);
});

app.Run();
