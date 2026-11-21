namespace Extraccion.Infraestructura;

// Todo lo configurable vive en appsettings.json (seccion "Extraccion").
public class OpcionesExtraccion
{
    public string CarpetaStaging { get; set; } = "staging";
    public OpcionesCsv Csv { get; set; } = new();
    public OpcionesBaseDatos BaseDatos { get; set; } = new();
    public OpcionesApi Api { get; set; } = new();
}

public class OpcionesCsv
{
    public bool Habilitada { get; set; } = true;
    public string Ruta { get; set; } = "data/surveys_part1.csv";
    public string[] EncabezadosRequeridos { get; set; } = Array.Empty<string>();
}

public class OpcionesBaseDatos
{
    public bool Habilitada { get; set; } = true;
    // No se guarda en el JSON: viene de una variable de entorno
    public string CadenaConexion { get; set; } = "";
    public string Canal { get; set; } = "Web";
}

public class OpcionesApi
{
    public bool Habilitada { get; set; } = true;
    public string UrlBase { get; set; } = "";
    public string Ruta { get; set; } = "api/comentarios";
    // Opcional: tambien viene de una variable de entorno
    public string Token { get; set; } = "";
    public int TimeoutSegundos { get; set; } = 30;
}
