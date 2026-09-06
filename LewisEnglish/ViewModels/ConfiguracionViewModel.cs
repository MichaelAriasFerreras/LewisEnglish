using System.Windows;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// ViewModel de configuracion: permite al coach cambiar su correo y contrasena.
    /// </summary>
    public class ConfiguracionViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly Admin _admin;

        private string _email;
        private string _nombre;
        private string _mensaje = string.Empty;
        private bool _mensajeEsError;

        public ConfiguracionViewModel(DatabaseService db, Admin admin)
        {
            _db = db;
            _admin = admin;
            _email = admin.Email;
            _nombre = admin.Nombre;

            GuardarCommand = new RelayCommand(o => Guardar(o));
        }

        public RelayCommand GuardarCommand { get; }

        public System.Action? CredencialesActualizadas { get; set; }

        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Nombre { get => _nombre; set => SetProperty(ref _nombre, value); }

        public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            set
            {
                SetProperty(ref _mensajeEsError, value);
                OnPropertyChanged(nameof(ColorMensaje));
            }
        }

        public string ColorMensaje => MensajeEsError ? "#EF4444" : "#22C55E";

        public void Cargar()
        {
            Email = _admin.Email;
            Nombre = _admin.Nombre;
            Mensaje = string.Empty;
        }

        private void Guardar(object? parameter)
        {
            string password = string.Empty;
            string confirmacion = string.Empty;

            if (parameter is object[] datos && datos.Length >= 2)
            {
                password = datos[0]?.ToString() ?? string.Empty;
                confirmacion = datos[1]?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                Mostrar("El correo no puede estar vacio.", true);
                return;
            }

            if (!Email.Contains("@"))
            {
                Mostrar("Ingrese un correo valido.", true);
                return;
            }

            string passwordFinal = _admin.Password;
            if (!string.IsNullOrWhiteSpace(password) || !string.IsNullOrWhiteSpace(confirmacion))
            {
                if (password.Length < 4)
                {
                    Mostrar("La contrasena debe tener al menos 4 caracteres.", true);
                    return;
                }
                if (password != confirmacion)
                {
                    Mostrar("Las contrasenas no coinciden.", true);
                    return;
                }
                passwordFinal = password;
            }

            _db.CambiarCredenciales(_admin.Id, Email.Trim(), passwordFinal);

            _admin.Email = Email.Trim();
            _admin.Password = passwordFinal;
            _admin.Nombre = Nombre;

            CredencialesActualizadas?.Invoke();

            Mostrar("Credenciales actualizadas correctamente.", false);
        }

        private void Mostrar(string texto, bool esError)
        {
            MensajeEsError = esError;
            Mensaje = texto;
        }
    }
}
