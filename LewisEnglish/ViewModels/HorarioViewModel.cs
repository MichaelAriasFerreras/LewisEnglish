using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LewisEnglish.Helpers;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>Fila mostrada en la vista de horario.</summary>
    public class EstudianteHorario
    {
        public Estudiante Estudiante { get; set; } = new();
        public string Nombre => Estudiante.Nombre;
        public string RangoHorario => Estudiante.RangoHorario;
        public string FrecuenciaTexto => Estudiante.FrecuenciaTexto;
        public decimal Tarifa => Estudiante.Tarifa;
        public string EstadoPagoTexto { get; set; } = "Sin periodo";
    }

    /// <summary>
    /// Muestra los estudiantes que reciben clase en un dia (y franja) determinado.
    /// </summary>
    public class HorarioViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private DiaSemana _diaSeleccionado = DiaSemana.Lunes;
        private string _franjaSeleccionada = "Todas";

        public HorarioViewModel(DatabaseService db)
        {
            _db = db;
            WhatsAppCommand = new RelayCommand(p => EnviarWhatsApp(p as EstudianteHorario));
        }

        public ObservableCollection<EstudianteHorario> Clases { get; } = new();

        public Array DiasSemana => Enum.GetValues(typeof(DiaSemana));

        public List<string> Franjas
        {
            get
            {
                var lista = new List<string> { "Todas" };
                lista.AddRange(TimeHelper.FranjasHorarias());
                return lista;
            }
        }

        public DiaSemana DiaSeleccionado
        {
            get => _diaSeleccionado;
            set { if (SetProperty(ref _diaSeleccionado, value)) Filtrar(); }
        }

        public string FranjaSeleccionada
        {
            get => _franjaSeleccionada;
            set { if (SetProperty(ref _franjaSeleccionada, value)) Filtrar(); }
        }

        public RelayCommand WhatsAppCommand { get; }

        public void Cargar()
        {
            _db.GenerarPeriodos();
            Filtrar();
        }

        private void Filtrar()
        {
            Clases.Clear();
            var hoy = DateTime.Today;

            var estudiantes = _db.ObtenerEstudiantesActivos()
                .Where(e => e.DiaClase == DiaSeleccionado);

            if (FranjaSeleccionada != "Todas")
                estudiantes = estudiantes.Where(e => e.HoraInicio == FranjaSeleccionada);

            foreach (var e in estudiantes.OrderBy(e => TimeHelper.Minutos(e.HoraInicio)))
            {
                var pagos = _db.ObtenerPagosPorEstudiante(e.Id);
                var actual = pagos.FirstOrDefault(p =>
                    p.PeriodoInicio.Date <= hoy && p.PeriodoFin.Date >= hoy);

                string estado = actual != null ? actual.EstadoTexto
                    : (pagos.Any(p => p.Estado == EstadoPago.Vencido) ? "Vencido" : "Sin periodo");

                Clases.Add(new EstudianteHorario
                {
                    Estudiante = e,
                    EstadoPagoTexto = estado
                });
            }
        }

        private void EnviarWhatsApp(EstudianteHorario? item)
        {
            if (item?.Estudiante == null) return;
            var e = item.Estudiante;
            string telefono = string.IsNullOrWhiteSpace(e.WhatsApp) ? e.Telefono : e.WhatsApp;
            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show("El estudiante no tiene numero de WhatsApp.", "WhatsApp",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try { WhatsAppHelper.Enviar(telefono, WhatsAppHelper.ConstruirMensaje(e, e.Tarifa)); }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir WhatsApp.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
