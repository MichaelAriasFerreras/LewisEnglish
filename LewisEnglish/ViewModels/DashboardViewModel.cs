using System.Collections.ObjectModel;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// Panel principal con las metricas del negocio.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private int _estudiantesActivos;
        private decimal _cobradoMes;
        private decimal _pendienteMes;
        private decimal _esperadoMes;
        private int _estudiantesEnMora;
        private string _nombreMes = string.Empty;

        public DashboardViewModel(DatabaseService db)
        {
            _db = db;
        }

        public int EstudiantesActivos { get => _estudiantesActivos; set => SetProperty(ref _estudiantesActivos, value); }
        public decimal CobradoMes { get => _cobradoMes; set => SetProperty(ref _cobradoMes, value); }
        public decimal PendienteMes { get => _pendienteMes; set => SetProperty(ref _pendienteMes, value); }
        public decimal EsperadoMes { get => _esperadoMes; set => SetProperty(ref _esperadoMes, value); }
        public int EstudiantesEnMora { get => _estudiantesEnMora; set => SetProperty(ref _estudiantesEnMora, value); }
        public string NombreMes { get => _nombreMes; set => SetProperty(ref _nombreMes, value); }

        public ObservableCollection<IngresoMensual> IngresosPorMes { get; } = new();
        public ObservableCollection<Estudiante> ProximasClases { get; } = new();
        public ObservableCollection<Pago> PagosVencidos { get; } = new();

        public void Cargar()
        {
            // Genera los periodos pendientes y actualiza estados antes de calcular
            _db.GenerarPeriodos();

            var m = _db.ObtenerMetricasDashboard();

            EstudiantesActivos = m.EstudiantesActivos;
            CobradoMes = m.CobradoMes;
            PendienteMes = m.PendienteMes;
            EsperadoMes = m.EsperadoMes;
            EstudiantesEnMora = m.EstudiantesEnMora;
            NombreMes = m.NombreMes;

            IngresosPorMes.Clear();
            foreach (var i in m.IngresosPorMes) IngresosPorMes.Add(i);

            ProximasClases.Clear();
            foreach (var e in m.ProximasClases) ProximasClases.Add(e);

            PagosVencidos.Clear();
            foreach (var p in m.PagosVencidos) PagosVencidos.Add(p);
        }
    }
}
