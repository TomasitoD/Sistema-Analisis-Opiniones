using Extraccion.Aplicacion;
using Extraccion.Dominio;
using Extraccion.Infraestructura;
using Extraccion.Infraestructura.Extractores;
using Extraccion.Worker;

// appsettings.json se lee de la carpeta del programa. Las rutas relativas
// (data, staging) se cuentan desde donde se ejecuta el comando.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

var seccion = builder.Configuration.GetSection("Extraccion");
builder.Services.Configure<OpcionesExtraccion>(seccion);
var opciones = seccion.Get<OpcionesExtraccion>() ?? new OpcionesExtraccion();

// Cada fuente se registra solo si esta habilitada en la configuracion
if (opciones.Csv.Habilitada)
    builder.Services.AddSingleton<IExtractor, CsvExtractor>();

if (opciones.BaseDatos.Habilitada)
    builder.Services.AddSingleton<IExtractor, DatabaseExtractor>();

if (opciones.Api.Habilitada)
{
    builder.Services.AddHttpClient(ApiExtractor.NombreCliente, cliente =>
    {
        cliente.BaseAddress = new Uri(opciones.Api.UrlBase);
        cliente.Timeout = TimeSpan.FromSeconds(opciones.Api.TimeoutSegundos);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler { AllowAutoRedirect = false });

    builder.Services.AddSingleton<IExtractor, ApiExtractor>();
}

builder.Services.AddSingleton<IAlmacenStaging, AlmacenStagingArchivos>();
builder.Services.AddSingleton<ProcesoExtraccion>();
builder.Services.AddHostedService<ExtraccionWorker>();

var host = builder.Build();
await host.RunAsync();
return Environment.ExitCode;
