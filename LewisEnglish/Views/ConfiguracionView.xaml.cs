using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LewisEnglish.Data;
using LewisEnglish.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace LewisEnglish.Views
{
    public partial class ConfiguracionView : UserControl
    {
        public ConfiguracionView()
        {
            InitializeComponent();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfiguracionViewModel vm)
            {
                var datos = new object[] { PassBox.Password, PassConfirmBox.Password };
                if (vm.GuardarCommand.CanExecute(datos))
                    vm.GuardarCommand.Execute(datos);

                PassBox.Password = string.Empty;
                PassConfirmBox.Password = string.Empty;
            }
        }

        /// <summary>
        /// Guarda una copia de seguridad de la base de datos en la ubicacion elegida por el usuario.
        /// </summary>
        private void GuardarRespaldo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string origen = AppDbContext.DbPath;
                if (!File.Exists(origen))
                {
                    MessageBox.Show("Todavia no hay una base de datos que respaldar.", "Respaldo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Guardar copia de seguridad",
                    Filter = "Copia de seguridad Lewis (*.db)|*.db|Todos los archivos (*.*)|*.*",
                    FileName = $"LewisEnglish_respaldo_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (dlg.ShowDialog() != true) return;

                // Liberar cualquier bloqueo del archivo antes de copiar
                SqliteConnection.ClearAllPools();
                File.Copy(origen, dlg.FileName, overwrite: true);

                MensajeRespaldo.Text = $"Copia de seguridad guardada en:\n{dlg.FileName}";
                MessageBox.Show("La copia de seguridad se guardo correctamente.", "Respaldo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo guardar la copia de seguridad.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Restaura una copia de seguridad reemplazando la base de datos actual.
        /// La aplicacion se cierra al terminar para aplicar los cambios de forma segura.
        /// </summary>
        private void RestaurarRespaldo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Restaurar copia de seguridad",
                    Filter = "Copia de seguridad Lewis (*.db)|*.db|Todos los archivos (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dlg.ShowDialog() != true) return;

                var confirm = MessageBox.Show(
                    "Al restaurar esta copia se reemplazaran TODOS los datos actuales " +
                    "(estudiantes, pagos y facturas) por los del archivo seleccionado.\n\n" +
                    "Esta accion no se puede deshacer. La aplicacion se cerrara al terminar.\n\n" +
                    "Deseas continuar?",
                    "Restaurar copia de seguridad", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                string destino = AppDbContext.DbPath;

                // Liberar cualquier bloqueo del archivo antes de reemplazarlo
                SqliteConnection.ClearAllPools();
                File.Copy(dlg.FileName, destino, overwrite: true);

                MessageBox.Show(
                    "La copia de seguridad se restauro correctamente.\n\n" +
                    "La aplicacion se cerrara ahora. Vuelve a abrirla para ver los datos restaurados.",
                    "Restaurar copia de seguridad", MessageBoxButton.OK, MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo restaurar la copia de seguridad.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
