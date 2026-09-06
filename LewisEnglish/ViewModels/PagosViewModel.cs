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
    /// <summary>
    /// Registro de pagos por periodo y envio de recordatorios.
    /// </summary>
    public class PagosViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;

        private Pago? _seleccionado;
        private string _filtroEstado = "Todos";
        private FormaPago _formaPago = FormaPago.Efectivo;
        private string _banco = string.Empty;
        private DateTime _fechaPago = DateTime.Today;
        private string _mensaje = string.Empty;

        public PagosViewModel(DatabaseService db)
        {
            _db = db;
            RegistrarPagoCommand = new RelayCommand(_ => RegistrarPago(), _ => Seleccionado != null && Seleccionado.Estado != EstadoPago.Pagado);
            RecordatorioCommand = new RelayCommand(p => EnviarRecordatorio(p as Pago));
            RecordatoriosMasivosCommand = new RelayCommand(_ => EnviarRecordatoriosMasivos());
        }

        public ObservableCollection<Pago> Pagos { get; } = new();

        public List<string> FiltrosEstado => new() { "Todos", "Pendiente", "Vencido", "Pagado" };
        public Array FormasPago => Enum.GetValues(typeof(FormaPago));
        public List<string> Bancos => new()
        {
            "Banreservas", "Banco Popular", "BHD Leon", "Scotiabank",
            "Banco Santa Cruz", "Banco BDI", "Banco Caribe", "Banco Ademi",
            "Asociacion Popular (APAP)", "Asociacion Cibao (ACAP)", "Banesco", "Otro"
        };

        public Pago? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value) && value != null)
                {
                    FormaPago = value.FormaPago;
                    Banco = value.Banco;
                    OnPropertyChanged(nameof(FormaPago));
                    OnPropertyChanged(nameof(Banco));
                }
            }
        }

        public string FiltroEstado
        {
            get => _filtroEstado;
            set { if (SetProperty(ref _filtroEstado, value)) AplicarFiltro(); }
        }

        public FormaPago FormaPago { get => _formaPago; set => SetProperty(ref _formaPago, value); }
        public string Banco { get => _banco; set => SetProperty(ref _banco, value); }
        public DateTime FechaPago { get => _fechaPago; set => SetProperty(ref _fechaPago, value); }
        public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }

        public RelayCommand RegistrarPagoCommand { get; }
        public RelayCommand RecordatorioCommand { get; }
        public RelayCommand RecordatoriosMasivosCommand { get; }

        private List<Pago> _todos = new();

        public void Cargar()
        {
            _db.GenerarPeriodos();
            _todos = _db.ObtenerPagos();
            AplicarFiltro();
            Mensaje = string.Empty;
        }

        private void AplicarFiltro()
        {
            Pagos.Clear();
            IEnumerable<Pago> filtrados = _todos;
            filtrados = FiltroEstado switch
            {
                "Pendiente" => _todos.Where(p => p.Estado == EstadoPago.Pendiente),
                "Vencido" => _todos.Where(p => p.Estado == EstadoPago.Vencido),
                "Pagado" => _todos.Where(p => p.Estado == EstadoPago.Pagado),
                _ => _todos
            };
            foreach (var p in filtrados) Pagos.Add(p);
        }

        private void RegistrarPago()
        {
            if (Seleccionado == null) return;
            if (Seleccionado.Estado == EstadoPago.Pagado)
            {
                MessageBox.Show("Este periodo ya esta pagado.", "Pagos",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var factura = _db.RegistrarPago(Seleccionado.Id, FormaPago,
                FormaPago == FormaPago.Transferencia ? Banco : string.Empty, FechaPago);

            Mensaje = factura != null
                ? $"Pago registrado. Factura {factura.NumeroFactura} generada."
                : "Pago registrado.";

            Cargar();
        }

        private void EnviarRecordatorio(Pago? pago)
        {
            pago ??= Seleccionado;
            if (pago == null) return;

            var estudiante = _db.ObtenerEstudiantes().FirstOrDefault(e => e.Id == pago.EstudianteId);
            if (estudiante == null) return;

            string telefono = string.IsNullOrWhiteSpace(estudiante.WhatsApp)
                ? estudiante.Telefono : estudiante.WhatsApp;
            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show($"{estudiante.Nombre} no tiene numero de WhatsApp.", "WhatsApp",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try { WhatsAppHelper.Enviar(telefono, WhatsAppHelper.ConstruirMensaje(estudiante, pago.Monto)); }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir WhatsApp.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnviarRecordatoriosMasivos()
        {
            var pendientes = _db.ObtenerPagosPendientes();
            if (pendientes.Count == 0)
            {
                MessageBox.Show("No hay pagos pendientes o vencidos.", "Recordatorios",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var r = MessageBox.Show(
                $"Se abriran {pendientes.Count} conversaciones de WhatsApp (una por estudiante con pago pendiente). Continuar?",
                "Enviar recordatorios", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            var estudiantes = _db.ObtenerEstudiantes().ToDictionary(e => e.Id);

            // Un recordatorio por estudiante (el pago pendiente mas antiguo)
            var porEstudiante = pendientes
                .GroupBy(p => p.EstudianteId)
                .Select(g => g.OrderBy(p => p.PeriodoFin).First());

            int enviados = 0;
            foreach (var pago in porEstudiante)
            {
                if (!estudiantes.TryGetValue(pago.EstudianteId, out var est)) continue;
                string telefono = string.IsNullOrWhiteSpace(est.WhatsApp) ? est.Telefono : est.WhatsApp;
                if (string.IsNullOrWhiteSpace(telefono)) continue;

                try
                {
                    WhatsAppHelper.Enviar(telefono, WhatsAppHelper.ConstruirMensaje(est, pago.Monto));
                    enviados++;
                }
                catch { /* continuar con el resto */ }
            }

            Mensaje = $"Se abrieron {enviados} recordatorio(s) de WhatsApp.";
        }
    }
}
