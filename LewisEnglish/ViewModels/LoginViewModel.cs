using System;
using System.IO;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesion.
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private string _email = "lewis@ingles.com";
        private string _password = string.Empty;
        private string _mensajeError = string.Empty;
        private bool _recordarCorreo;

        /// <summary>Ruta del archivo local donde se recuerda el correo (%AppData%/LewisEnglish/login.settings).</summary>
        private static string RutaSettings
        {
            get
            {
                var carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LewisEnglish");
                Directory.CreateDirectory(carpeta);
                return Path.Combine(carpeta, "login.settings");
            }
        }

        public LoginViewModel(DatabaseService db)
        {
            _db = db;
            IngresarCommand = new RelayCommand(Ingresar);
            CargarCorreoRecordado();
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        /// <summary>Cuando esta marcado, el correo se guarda localmente y se precarga al abrir.</summary>
        public bool RecordarCorreo
        {
            get => _recordarCorreo;
            set => SetProperty(ref _recordarCorreo, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public RelayCommand IngresarCommand { get; }

        /// <summary>Se invoca cuando el login es correcto, entregando el admin autenticado.</summary>
        public Action<Admin>? LoginExitoso { get; set; }

        private void Ingresar(object? parameter)
        {
            if (parameter is string pwd && !string.IsNullOrEmpty(pwd))
                Password = pwd;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MensajeError = "Ingrese su correo y contrasena.";
                return;
            }

            var admin = _db.ValidarLogin(Email.Trim(), Password);
            if (admin == null)
            {
                MensajeError = "Correo o contrasena incorrectos.";
                return;
            }

            MensajeError = string.Empty;
            GuardarCorreoRecordado();
            LoginExitoso?.Invoke(admin);
        }

        private void CargarCorreoRecordado()
        {
            try
            {
                if (File.Exists(RutaSettings))
                {
                    var correo = File.ReadAllText(RutaSettings).Trim();
                    if (!string.IsNullOrWhiteSpace(correo))
                    {
                        Email = correo;
                        RecordarCorreo = true;
                    }
                }
            }
            catch { /* si falla la lectura, se usa el correo por defecto */ }
        }

        private void GuardarCorreoRecordado()
        {
            try
            {
                if (RecordarCorreo)
                    File.WriteAllText(RutaSettings, Email.Trim());
                else if (File.Exists(RutaSettings))
                    File.Delete(RutaSettings);
            }
            catch { /* no bloquear el login si falla el guardado */ }
        }
    }
}
