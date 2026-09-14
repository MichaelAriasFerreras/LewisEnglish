using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
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

            // 1) Cerrar la aplicacion si esta abierta (evita el error "el archivo esta
            //    siendo usado por otro proceso" al sobrescribir LewisEnglish.exe).
            progreso?.Report("Cerrando la aplicacion si esta abierta...");
            CerrarAplicacion();

            // 2) Limpiar cualquier archivo temporal .old de una actualizacion anterior.
            LimpiarArchivosViejos();

            string tempZip = Path.Combine(Path.GetTempPath(),
                $"LewisEnglish_update_{DateTime.Now:yyyyMMddHHmmss}.zip");

            progreso?.Report("Descargando actualizacion...");
            var bytes = await Http.GetByteArrayAsync(info.url);
            await File.WriteAllBytesAsync(tempZip, bytes);

            progreso?.Report("Instalando archivos...");
            // Extraer sobrescribiendo (con reintentos y renombrado si algun archivo sigue en uso).
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

                    ExtraerConReintento(entry, destino);
                }
            }

            // Actualizar version instalada
            await File.WriteAllTextAsync(VersionFile, info.version.Trim());

            try { File.Delete(tempZip); } catch { }

            // Intentar borrar los .old ya liberados (best-effort).
            LimpiarArchivosViejos();

            progreso?.Report("Actualizacion completada.");
        }

        /// <summary>
        /// Cierra cualquier instancia en ejecucion de la aplicacion (LewisEnglish.exe).
        /// Primero intenta un cierre normal y, si no responde, la finaliza. Asi se libera
        /// el ejecutable para poder sobrescribirlo durante la actualizacion.
        /// </summary>
        private static void CerrarAplicacion()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("LewisEnglish"))
                {
                    try
                    {
                        if (p.HasExited) continue;
                        p.CloseMainWindow();
                        if (!p.WaitForExit(3000))
                        {
                            p.Kill(true);
                            p.WaitForExit(3000);
                        }
                    }
                    catch { /* ignorar: puede haber cerrado sola */ }
                    finally { p.Dispose(); }
                }
                // Pequena pausa para que Windows libere por completo el archivo.
                Thread.Sleep(500);
            }
            catch { /* no bloquear la actualizacion por esto */ }
        }

        /// <summary>
        /// Extrae un archivo del ZIP sobre su destino. Si el archivo esta en uso o
        /// bloqueado, lo renombra a ".old_..." (Windows permite mover un archivo en uso
        /// aunque no sobrescribirlo) y reintenta, garantizando que la actualizacion no falle.
        /// </summary>
        private static void ExtraerConReintento(ZipArchiveEntry entry, string destino)
        {
            const int maxIntentos = 6;
            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                try
                {
                    entry.ExtractToFile(destino, overwrite: true);
                    return;
                }
                catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) && intento < maxIntentos)
                {
                    // El archivo destino esta en uso: renombrarlo para liberar la ruta.
                    try
                    {
                        if (File.Exists(destino))
                        {
                            string viejo = destino + ".old_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            File.Move(destino, viejo);
                        }
                    }
                    catch { /* si no se puede renombrar, esperar y reintentar */ }

                    Thread.Sleep(400);
                }
            }

            // Ultimo intento: si falla aqui, la excepcion se propaga y se informa al usuario.
            entry.ExtractToFile(destino, overwrite: true);
        }

        /// <summary>Elimina los archivos temporales ".old_" dejados por actualizaciones previas.</summary>
        private void LimpiarArchivosViejos()
        {
            try
            {
                if (!Directory.Exists(AppDir)) return;
                foreach (var f in Directory.GetFiles(AppDir, "*.old_*", SearchOption.AllDirectories))
                {
                    try { File.Delete(f); } catch { /* aun en uso: se borrara la proxima vez */ }
                }
            }
            catch { /* best-effort */ }
        }
    }
}
