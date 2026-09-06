using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LewisEnglishLauncher
{
    /// <summary>
    /// Informacion de la ultima version publicada (leida de update.json en GitHub).
    /// </summary>
    public class UpdateInfo
    {
        public string version { get; set; } = "0.0.0";
        public string url { get; set; } = string.Empty;
        public string notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Servicio que consulta GitHub por nuevas versiones, compara con la version
    /// local instalada y descarga/instala la actualizacion de la aplicacion.
    ///
    /// Distribucion esperada (carpeta que recibe el cliente):
    ///   LewisEnglish\
    ///     LewisEnglishLauncher.exe
    ///     app\
    ///        LewisEnglish.exe   (+ archivos)
    ///        lewis_logo.png
    ///        version.txt        (version instalada, ej. "1.0.0")
    /// </summary>
    public class UpdateService
    {
        // URL del manifiesto de actualizaciones (rama main del repositorio).
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/MichaelAriasFerreras/LewisEnglish/main/update.json";

        private static readonly HttpClient Http = CrearHttpClient();

        private static HttpClient CrearHttpClient()
        {
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromMinutes(10);
            c.DefaultRequestHeaders.Add("User-Agent", "LewisEnglishLauncher");
            return c;
        }

        /// <summary>Carpeta donde esta instalada la aplicacion (junto al launcher, subcarpeta "app").</summary>
        public string AppDir => Path.Combine(AppContext.BaseDirectory, "app");

        /// <summary>Ruta del ejecutable principal de la aplicacion.</summary>
        public string AppExe => Path.Combine(AppDir, "LewisEnglish.exe");

        /// <summary>Ruta del archivo de version instalada.</summary>
        public string VersionFile => Path.Combine(AppDir, "version.txt");

        /// <summary>Lee la version instalada localmente. Devuelve 0.0.0 si no existe.</summary>
        public string ObtenerVersionLocal()
        {
            try
            {
                if (File.Exists(VersionFile))
                {
                    string v = File.ReadAllText(VersionFile).Trim();
                    return string.IsNullOrWhiteSpace(v) ? "0.0.0" : v;
                }
            }
            catch { }
            return "0.0.0";
        }

        /// <summary>Consulta GitHub por la ultima version publicada.</summary>
        public async Task<UpdateInfo?> ObtenerUltimaVersionAsync()
        {
            try
            {
                // Evita cache anadiendo un parametro de tiempo
                string url = ManifestUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = await Http.GetStringAsync(url);
                var info = JsonSerializer.Deserialize<UpdateInfo>(json);
                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>True si la version remota es mayor que la local.</summary>
        public bool HayActualizacion(string local, string remota)
        {
            if (Version.TryParse(local, out var vl) && Version.TryParse(remota, out var vr))
                return vr > vl;
            // Si no se pueden parsear, comparar como texto
            return !string.Equals(local, remota, StringComparison.OrdinalIgnoreCase)
                   && !string.IsNullOrWhiteSpace(remota);
        }

        /// <summary>
        /// Descarga el ZIP de la nueva version y lo extrae sobre la carpeta "app",
        /// sobrescribiendo los archivos. Luego actualiza version.txt.
        /// </summary>
        public async Task InstalarActualizacionAsync(UpdateInfo info, IProgress<string>? progreso = null)
        {
            if (string.IsNullOrWhiteSpace(info.url))
                throw new InvalidOperationException("El manifiesto no contiene una URL de descarga.");

            Directory.CreateDirectory(AppDir);

            string tempZip = Path.Combine(Path.GetTempPath(),
                $"LewisEnglish_update_{DateTime.Now:yyyyMMddHHmmss}.zip");

            progreso?.Report("Descargando actualizacion...");
            var bytes = await Http.GetByteArrayAsync(info.url);
            await File.WriteAllBytesAsync(tempZip, bytes);

            progreso?.Report("Instalando archivos...");
            // Extraer sobrescribiendo
            using (var archive = ZipFile.OpenRead(tempZip))
            {
                foreach (var entry in archive.Entries)
                {
                    // Carpetas
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(Path.Combine(AppDir, entry.FullName));
                        continue;
                    }

                    string destino = Path.Combine(AppDir, entry.FullName);
                    string? dir = Path.GetDirectoryName(destino);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    entry.ExtractToFile(destino, overwrite: true);
                }
            }

            // Actualizar version instalada
            await File.WriteAllTextAsync(VersionFile, info.version.Trim());

            try { File.Delete(tempZip); } catch { }

            progreso?.Report("Actualizacion completada.");
        }
    }
}
