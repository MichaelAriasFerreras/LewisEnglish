using System;
using System.Collections.ObjectModel;
using System.Windows;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// Registro y consulta de asistencia por estudiante.
    /// </summary>
    public class AsistenciaViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private Estudiante? _estudianteSeleccionado;
        private DateTime _fecha = DateTime.Today;
        private EstadoAsistencia _estado = EstadoAsistencia.Presente;
        private string _nota = string.Empty;
        private string _mensaje = string.Empty;

        public AsistenciaViewModel(DatabaseService db)
        {
            _db = db;
            RegistrarCommand = new RelayCommand(_ => Registrar(), _ => EstudianteSeleccionado != null);
        }

        public ObservableCollection<Estudiante> Estudiantes { get; } = new();
        public ObservableCollection<Asistencia> Historial { get; } = new();

        public Array Estados => Enum.GetValues(typeof(EstadoAsistencia));

        public Estudiante? EstudianteSeleccionado
        {
            get => _estudianteSeleccionado;
            set
            {
                if (SetProperty(ref _estudianteSeleccionado, value))
                    CargarHistorial();
            }
        }

        public DateTime Fecha { get => _fecha; set => SetProperty(ref _fecha, value); }
        public EstadoAsistencia Estado { get => _estado; set => SetProperty(ref _estado, value); }
        public string Nota { get => _nota; set => SetProperty(ref _nota, value); }
        public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }

        public RelayCommand RegistrarCommand { get; }

        public void Cargar()
        {
            Estudiantes.Clear();
            foreach (var e in _db.ObtenerEstudiantesActivos())
                Estudiantes.Add(e);
            Historial.Clear();
            Mensaje = string.Empty;
        }

        private void CargarHistorial()
        {
            Historial.Clear();
            if (EstudianteSeleccionado == null) return;
            foreach (var a in _db.ObtenerAsistenciasPorEstudiante(EstudianteSeleccionado.Id))
                Historial.Add(a);
        }

        private void Registrar()
        {
            if (EstudianteSeleccionado == null) return;

            _db.RegistrarAsistencia(EstudianteSeleccionado.Id, Fecha, Estado, Nota ?? string.Empty);
            Mensaje = $"Asistencia registrada: {Estado} - {Fecha:dd/MM/yyyy}";
            Nota = string.Empty;
            CargarHistorial();
        }
    }
}
