using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using LewisEnglish.Helpers;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// Gestion de perfiles de estudiantes (alta, edicion, baja, recordatorio).
    /// </summary>
    public class EstudiantesViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private Estudiante? _seleccionado;
        private Estudiante _editable = new();
        private bool _mostrarFormulario;
        private string _tituloFormulario = "Nuevo estudiante";

        public EstudiantesViewModel(DatabaseService db)
        {
            _db = db;

            NuevoCommand = new RelayCommand(_ => Nuevo());
            EditarCommand = new RelayCommand(_ => Editar(), _ => Seleccionado != null);
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => MostrarFormulario = false);
            EliminarCommand = new RelayCommand(_ => Eliminar(), _ => Seleccionado != null);
            WhatsAppCommand = new RelayCommand(p => EnviarWhatsApp(p as Estudiante));
        }

        public ObservableCollection<Estudiante> Estudiantes { get; } = new();

        public Estudiante? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value))
                {
                    CargarDetalle();
                    OnPropertyChanged(nameof(HaySeleccion));
                    OnPropertyChanged(nameof(TienePagosVencidos));
                }
            }
        }

        /// <summary>True cuando hay un estudiante seleccionado (para mostrar el panel de detalle).</summary>
        public bool HaySeleccion => _seleccionado != null;

        /// <summary>Historial de pagos del estudiante seleccionado (mas recientes primero).</summary>
        public ObservableCollection<Pago> PagosSeleccionado { get; } = new();

        /// <summary>Historial de asistencias del estudiante seleccionado (mas recientes primero).</summary>
        public ObservableCollection<Asistencia> AsistenciasSeleccionado { get; } = new();

        /// <summary>True si el estudiante seleccionado tiene pagos vencidos.</summary>
        public bool TienePagosVencidos
        {
            get
            {
                foreach (var p in PagosSeleccionado)
                    if (p.Estado == EstadoPago.Vencido) return true;
                return false;
            }
        }

        private void CargarDetalle()
        {
            PagosSeleccionado.Clear();
            AsistenciasSeleccionado.Clear();
            if (_seleccionado == null) return;

            foreach (var p in _db.ObtenerPagosPorEstudiante(_seleccionado.Id))
                PagosSeleccionado.Add(p);
            foreach (var a in _db.ObtenerAsistenciasPorEstudiante(_seleccionado.Id))
                AsistenciasSeleccionado.Add(a);
        }

        public Estudiante Editable
        {
            get => _editable;
            set => SetProperty(ref _editable, value);
        }

        public bool MostrarFormulario
        {
            get => _mostrarFormulario;
            set => SetProperty(ref _mostrarFormulario, value);
        }

        public string TituloFormulario
        {
            get => _tituloFormulario;
            set => SetProperty(ref _tituloFormulario, value);
        }

        // ---- Fuentes para los ComboBox ----
        public Array DiasSemana => Enum.GetValues(typeof(DiaSemana));
        public Array Frecuencias => Enum.GetValues(typeof(FrecuenciaPago));
        public Array FormasPago => Enum.GetValues(typeof(FormaPago));
        public Array Estados => Enum.GetValues(typeof(EstadoEstudiante));
        public List<string> Franjas => TimeHelper.FranjasHorarias();
        public List<double> Duraciones => new() { 0.5, 1, 1.5, 2, 2.5, 3 };
        public List<string> Bancos => new()
        {
            "Banreservas", "Banco Popular", "BHD Leon", "Scotiabank",
            "Banco Santa Cruz", "Banco BDI", "Banco Caribe", "Banco Ademi",
            "Asociacion Popular (APAP)", "Asociacion Cibao (ACAP)", "Banesco", "Otro"
        };

        public RelayCommand NuevoCommand { get; }
        public RelayCommand EditarCommand { get; }
        public RelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }
        public RelayCommand EliminarCommand { get; }
        public RelayCommand WhatsAppCommand { get; }

        public void Cargar()
        {
            Estudiantes.Clear();
            foreach (var e in _db.ObtenerEstudiantes())
                Estudiantes.Add(e);
            MostrarFormulario = false;
        }

        private void Nuevo()
        {
            Editable = new Estudiante();
            TituloFormulario = "Nuevo estudiante";
            MostrarFormulario = true;
        }

        private void Editar()
        {
            if (Seleccionado == null) return;
            Editable = Clonar(Seleccionado);
            TituloFormulario = "Editar estudiante";
            MostrarFormulario = true;
        }

        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(Editable.Nombre))
            {
                MessageBox.Show("El nombre del estudiante es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Editable.Tarifa <= 0)
            {
                MessageBox.Show("Ingrese una tarifa valida (mayor que cero).", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db.GuardarEstudiante(Editable);
            MostrarFormulario = false;
            Cargar();
        }

        private void Eliminar()
        {
            if (Seleccionado == null) return;
            var r = MessageBox.Show(
                $"Eliminar al estudiante \"{Seleccionado.Nombre}\"? Se borraran tambien sus pagos y asistencias.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r == MessageBoxResult.Yes)
            {
                _db.EliminarEstudiante(Seleccionado.Id);
                Cargar();
            }
        }

        private void EnviarWhatsApp(Estudiante? estudiante)
        {
            estudiante ??= Seleccionado;
            if (estudiante == null) return;

            string telefono = string.IsNullOrWhiteSpace(estudiante.WhatsApp)
                ? estudiante.Telefono : estudiante.WhatsApp;

            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show("El estudiante no tiene numero de telefono/WhatsApp.", "WhatsApp",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string mensaje = WhatsAppHelper.ConstruirMensaje(estudiante, estudiante.Tarifa);
            try { WhatsAppHelper.Enviar(telefono, mensaje); }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir WhatsApp.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Estudiante Clonar(Estudiante e) => new()
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Telefono = e.Telefono,
            WhatsApp = e.WhatsApp,
            Direccion = e.Direccion,
            Email = e.Email,
            DiaClase = e.DiaClase,
            HoraInicio = e.HoraInicio,
            Frecuencia = e.Frecuencia,
            Tarifa = e.Tarifa,
            FormaPago = e.FormaPago,
            Banco = e.Banco,
            Estado = e.Estado,
            FechaRegistro = e.FechaRegistro
        };
    }
}
