using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace LewisEnglishLauncher
{
    public partial class MainWindow : Window
    {
        private readonly UpdateService _svc = new();
        private UpdateInfo? _ultima;
        private string _versionLocal = "0.0.0";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) => await BuscarActualizacionesAsync();
        }

        private async Task BuscarActualizacionesAsync()
        {
            _versionLocal = _svc.ObtenerVersionLocal();
            TxtVersionLocal.Text = "v" + _versionLocal;
            TxtVersionRemota.Text = "...";
            TxtNotas.Text = string.Empty;
            BtnActualizar.IsEnabled = false;
            TxtEstado.Text = "Buscando actualizaciones...";
            TxtEstado.Foreground = System.Windows.Media.Brushes.White;
            MostrarBarra(true, indeterminado: true);

            _ultima = await _svc.ObtenerUltimaVersionAsync();

            MostrarBarra(false);

            if (_ultima == null)
            {
                TxtVersionRemota.Text = "-";
                TxtEstado.Text = "No se pudo conectar con el servidor de actualizaciones. " +
                                 "Verifica tu conexion a internet e intenta de nuevo. " +
                                 "Puedes abrir la aplicacion normalmente.";
                return;
            }

            TxtVersionRemota.Text = "v" + _ultima.version;
            TxtNotas.Text = string.IsNullOrWhiteSpace(_ultima.notes) ? string.Empty : "Novedades: " + _ultima.notes;

            if (_svc.HayActualizacion(_versionLocal, _ultima.version))
            {
                TxtEstado.Text = $"Actualizacion disponible v{_ultima.version}. " +
                                 "Presiona 'Actualizar ahora' para instalarla.";
                TxtEstado.Foreground = System.Windows.Media.Brushes.Gold;
                BtnActualizar.IsEnabled = true;
            }
            else
            {
                TxtEstado.Text = "Tu aplicacion esta actualizada. Puedes abrirla con normalidad.";
                TxtEstado.Foreground = System.Windows.Media.Brushes.White;
                BtnActualizar.IsEnabled = false;
            }
        }

        private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            await BuscarActualizacionesAsync();
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (_ultima == null)
                return;

            // --- Dialogo de confirmacion obligatorio antes de actualizar ---
            string notas = string.IsNullOrWhiteSpace(_ultima.notes)
                ? string.Empty
                : "\n\nNovedades de esta version:\n" + _ultima.notes;

            var confirmar = MessageBox.Show(
                $"¿Esta seguro que desea actualizar a la version v{_ultima.version}?{notas}\n\n" +
                "La aplicacion se descargara e instalara. No cierre el actualizador durante el proceso.",
                "Confirmar actualizacion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (confirmar != MessageBoxResult.Yes)
            {
                TxtEstado.Text = "Actualizacion cancelada. Puedes actualizar mas tarde o abrir la version actual.";
                TxtEstado.Foreground = System.Windows.Media.Brushes.White;
                return;
            }

            BtnActualizar.IsEnabled = false;
            BtnBuscar.IsEnabled = false;
            BtnAbrir.IsEnabled = false;
            MostrarBarra(true, indeterminado: true);

            try
            {
                var progreso = new Progress<string>(msg => TxtEstado.Text = msg);
                await _svc.InstalarActualizacionAsync(_ultima, progreso);

                _versionLocal = _svc.ObtenerVersionLocal();
                TxtVersionLocal.Text = "v" + _versionLocal;
                TxtEstado.Text = $"Actualizacion instalada correctamente (v{_versionLocal}). " +
                                 "Ya puedes abrir la aplicacion.";
                TxtEstado.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                TxtEstado.Text = "Ocurrio un error al actualizar: " + ex.Message +
                                 " Puedes intentar de nuevo o abrir la version actual.";
                TxtEstado.Foreground = System.Windows.Media.Brushes.Salmon;
                BtnActualizar.IsEnabled = true;
            }
            finally
            {
                MostrarBarra(false);
                BtnBuscar.IsEnabled = true;
                BtnAbrir.IsEnabled = true;
            }
        }

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(_svc.AppExe))
                {
                    MessageBox.Show(
                        "No se encontro la aplicacion en:\n" + _svc.AppExe +
                        "\n\nSi es la primera vez, presiona 'Actualizar ahora' para instalarla.",
                        "Aplicacion no encontrada", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _svc.AppExe,
                    WorkingDirectory = _svc.AppDir,
                    UseShellExecute = true
                });

                // Cierra el launcher al abrir la app
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir la aplicacion: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarBarra(bool visible, bool indeterminado = false)
        {
            Barra.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            Barra.IsIndeterminate = visible && indeterminado;
        }
    }
}
