# Sistema de Análisis de Opiniones - Extracción (Worker Service .NET 8)

Actividad 1 de Electiva I (ITLA). Es la **E** del proceso ETL: un Worker Service que extrae opiniones de tres fuentes distintas y las guarda en un staging de archivos.

| Fuente | Qué contiene | Cómo se extrae |
|---|---|---|
| CSV | Encuestas internas (`data/surveys_part1.csv`) | `CsvExtractor` con CsvHelper |
| Base de datos relacional | Reseñas web (canal Web) en MySQL | `DatabaseExtractor` con MySqlConnector |
| API REST | Comentarios de redes sociales | `ApiExtractor` con `IHttpClientFactory` |

## Estructura

```
src/
  Extraccion.Dominio          interfaces (IExtractor, IAlmacenStaging) y modelos
  Extraccion.Aplicacion       ProcesoExtraccion: coordina las fuentes en paralelo
  Extraccion.Infraestructura  extractores, configuración y staging en archivos
  Extraccion.Worker           Worker Service (Program.cs, appsettings.json)
  Extraccion.ApiDemo          API local que publica data/social_comments.csv
data/                         los CSV de origen
database/00_usuario_lectura.sql   usuario MySQL de solo lectura
```

## Requisitos

- SDK de .NET 8 o uno más nuevo (si solo está .NET 9 o 10, el programa usa ese runtime gracias a `RollForward`)
- MySQL con la base `AnalisisOpinionesClientes` ya cargada (entrega anterior del ETL)

## Cómo ejecutarlo

Todos los comandos se ejecutan desde la carpeta raíz del repositorio.

1. Crear el usuario de solo lectura (cambiar la clave dentro del script):

   ```
   mysql -u root -p < database/00_usuario_lectura.sql
   ```

2. Definir la cadena de conexión como variable de entorno (no se guarda en el repositorio):

   ```
   export Extraccion__BaseDatos__CadenaConexion='Server=localhost;Database=AnalisisOpinionesClientes;User=etl_extraccion;Password=TU_CLAVE;'
   ```

3. En una terminal, levantar la API de demostración:

   ```
   dotnet run --project src/Extraccion.ApiDemo
   ```

4. En otra terminal (con la variable del paso 2), ejecutar el Worker:

   ```
   dotnet run --project src/Extraccion.Worker
   ```

El resultado queda en `staging/<fecha-hora>-<id>/`: un archivo `.jsonl` por fuente y un `resumen.json`. Si algo falla, la carpeta de esa corrida se borra y el programa termina con código 1.

## Configuración

Todo está en `src/Extraccion.Worker/appsettings.json`: rutas, canal a consultar, URL de la API y timeout. Cada fuente tiene un campo `Habilitada` para activarla o apagarla. Las credenciales (cadena de conexión y token de la API) no van en el archivo: se pasan con variables de entorno `Extraccion__BaseDatos__CadenaConexion` y `Extraccion__Api__Token`.

## Códigos de salida

| Código | Significado |
|---|---|
| 0 | Extracción completa |
| 1 | Falló alguna fuente |
| 130 | Cancelada (Ctrl+C) |
