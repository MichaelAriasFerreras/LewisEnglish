using System;
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

        public LoginViewModel(DatabaseService db)
        {
            _db = db;
            IngresarCommand = new RelayCommand(Ingresar);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
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
            LoginExitoso?.Invoke(admin);
        }
    }
}
