using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ASO_RTR.Desktop.Configuration;

/// <summary>
/// Punto unico de acceso a la configuracion de la aplicacion.
/// Carga appsettings.json (valores por defecto, en el repo) y superpone
/// appsettings.local.json (por maquina, en .gitignore) si existe.
/// </summary>
public static class AppConfig
{
    private static readonly IConfigurationRoot _config = Build();

    private static IConfigurationRoot Build()
    {
        // La cadena por defecto usa LocalDB con |DataDirectory|, que SqlClient resuelve contra
        // esta ruta. Es un archivo por maquina (ver .gitignore): cada quien tiene el suyo, nadie
        // depende de la PC de otro para poder conectarse.
        Directory.CreateDirectory(CarpetaDatos);
        AppDomain.CurrentDomain.SetData("DataDirectory", CarpetaDatos);

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .Build();
    }

    /// <summary>
    /// Carpeta del archivo LocalDB (App_Data), siempre junto al ejecutable que corre en este
    /// momento — no subiendo un numero fijo de niveles.
    ///
    /// Antes subia tres niveles desde AppContext.BaseDirectory a proposito, para que
    /// "dotnet run"/F5 (bin/Debug/netX.0-windows) y el publish portable
    /// (EjecutablePortable/Release/win-x64) cayeran los dos en ASO_RTR.Desktop/App_Data: los dos
    /// estan exactamente tres niveles bajo ASO_RTR.Desktop/ dentro del repo. Eso se rompia en
    /// cuanto alguien copiaba SOLO la carpeta del ejecutable portable a otra maquina -que es todo
    /// el sentido de que sea portable-: las tres subidas caian en una carpeta cualquiera del
    /// sistema ajeno, LocalDB no podia adjuntar el .mdf ahi, y la migracion tronaba con un error
    /// que apuntaba a la cadena de conexion sin serlo. Junto al ejecutable, siempre, es lo unico
    /// que de verdad funciona pegue donde se pegue el .exe.
    /// </summary>
    private static string CarpetaDatos => Path.Combine(AppContext.BaseDirectory, "App_Data");

    /// <summary>Cadena de conexion a SQL Server (clave ConnectionStrings:AsoRtrDb).</summary>
    public static string ConnectionString =>
        _config.GetConnectionString("AsoRtrDb")
        ?? throw new InvalidOperationException(
            "No se encontro la cadena de conexion 'AsoRtrDb'. " +
            "Revisa appsettings.json o crea appsettings.local.json a partir de appsettings.local.example.json.");
}
