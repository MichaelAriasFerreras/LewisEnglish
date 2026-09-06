using System;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// ViewModel principal (shell). Gestiona la navegacion del sidebar.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private BaseViewModel? _currentViewModel;
        private string _seccionActiva = "Dashboard";

        public DashboardViewModel DashboardVM { get; }
        public EstudiantesViewModel EstudiantesVM { get; }
        public HorarioViewModel HorarioVM { get; }
        public AsistenciaViewModel AsistenciaVM { get; }
        public PagosViewModel PagosVM { get; }
        public FacturasViewModel FacturasVM { get; }
        public ConfiguracionViewModel ConfiguracionVM { get; }

        public Admin AdminActual { get; }

        public Action? CerrarSesion { get; set; }

        public MainViewModel(DatabaseService db, Admin admin)
        {
            _db = db;
            AdminActual = admin;

            DashboardVM = new DashboardViewModel(db);
            EstudiantesVM = new EstudiantesViewModel(db);
            HorarioVM = new HorarioViewModel(db);
            AsistenciaVM = new AsistenciaViewModel(db);
            PagosVM = new PagosViewModel(db);
            FacturasVM = new FacturasViewModel(db);
            ConfiguracionVM = new ConfiguracionViewModel(db, admin);
            ConfiguracionVM.CredencialesActualizadas = () =>
            {
                OnPropertyChanged(nameof(NombreAdmin));
                OnPropertyChanged(nameof(InicialAdmin));
            };

            NavegarCommand = new RelayCommand(p => Navegar(p?.ToString()));
            SalirCommand = new RelayCommand(() => CerrarSesion?.Invoke());

            Navegar("Dashboard");
        }

        public RelayCommand NavegarCommand { get; }
        public RelayCommand SalirCommand { get; }

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string SeccionActiva
        {
            get => _seccionActiva;
            set => SetProperty(ref _seccionActiva, value);
        }

        public string NombreAdmin => AdminActual?.Nombre ?? "Lewis";
        public string InicialAdmin => string.IsNullOrWhiteSpace(NombreAdmin) ? "L" : NombreAdmin.Substring(0, 1).ToUpper();

        private void Navegar(string? seccion)
        {
            switch (seccion)
            {
                case "Estudiantes":
                    EstudiantesVM.Cargar();
                    CurrentViewModel = EstudiantesVM;
                    SeccionActiva = "Estudiantes";
                    break;
                case "Horario":
                    HorarioVM.Cargar();
                    CurrentViewModel = HorarioVM;
                    SeccionActiva = "Horario";
                    break;
                case "Asistencia":
                    AsistenciaVM.Cargar();
                    CurrentViewModel = AsistenciaVM;
                    SeccionActiva = "Asistencia";
                    break;
                case "Pagos":
                    PagosVM.Cargar();
                    CurrentViewModel = PagosVM;
                    SeccionActiva = "Pagos";
                    break;
                case "Facturas":
                    FacturasVM.Cargar();
                    CurrentViewModel = FacturasVM;
                    SeccionActiva = "Facturas";
                    break;
                case "Configuracion":
                    ConfiguracionVM.Cargar();
                    CurrentViewModel = ConfiguracionVM;
                    SeccionActiva = "Configuracion";
                    break;
                default:
                    DashboardVM.Cargar();
                    CurrentViewModel = DashboardVM;
                    SeccionActiva = "Dashboard";
                    break;
            }
        }
    }
}
