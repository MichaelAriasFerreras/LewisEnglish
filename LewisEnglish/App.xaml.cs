using System;
using System.Windows;
using System.Windows.Threading;
using LewisEnglish.Services;
using LewisEnglish.ViewModels;
using LewisEnglish.Views;

namespace LewisEnglish
{
    /// <summary>
    /// Punto de entrada de la aplicacion "Lewis, English Speaking Coach".
    /// </summary>
    public partial class App : Application
    {
        public static DatabaseService Db { get; private set; } = new DatabaseService();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Capturar cualquier error no controlado y mostrarlo en lugar de cerrar la app en silencio
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Inicializar la base de datos SQLite y el admin por defecto
                Db.InicializarBaseDeDatos();
                MostrarLogin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo iniciar la aplicacion.\n\nDetalle: " + ex.Message,
                    "Error al iniciar", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "Ocurrio un error inesperado.\n\nDetalle: " + e.Exception.Message,
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                "Ocurrio un error inesperado.\n\nDetalle: " + (ex?.Message ?? "desconocido"),
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra la ventana de login y, tras un ingreso exitoso, abre la ventana principal.
        /// </summary>
        private void MostrarLogin()
        {
            var loginVM = new LoginViewModel(Db);
            var loginWindow = new LoginView { DataContext = loginVM };

            loginVM.LoginExitoso = admin =>
            {
                var mainVM = new MainViewModel(Db, admin);
                var main = new MainWindow { DataContext = mainVM };

                // Cerrar sesion: cierra la ventana principal y vuelve al login
                mainVM.CerrarSesion = () =>
                {
                    MostrarLogin();
                    main.Close();
                };

                MainWindow = main;
                main.Show();
                loginWindow.Close();
            };

            loginWindow.Show();
        }
    }
}
