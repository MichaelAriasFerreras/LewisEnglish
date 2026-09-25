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
        private decimal _montoEditable;
        private decimal _montoAbono;
        private decimal _montoPagadoEditable;
        private DateTime? _fechaVencimientoEditable;
        private string _notaEditable = string.Empty;
        private bool _pagoAdelantadoEditable;

        public PagosViewModel(DatabaseService db)
        {
            _db = db;
            RegistrarPagoCommand = new RelayCommand(_ => RegistrarPago(), _ => Seleccionado != null && Seleccionado.Estado != EstadoPago.Pagado);
            GuardarCambiosCommand = new RelayCommand(_ => GuardarCambios(), _ => Seleccionado != null);
            RecordatorioCommand = new RelayCommand(p => EnviarRecordatorio(p as Pago));
            RecordatoriosMasivosCommand = new RelayCommand(_ => EnviarRecordatoriosMasivos());
        }

        public ObservableCollection<Pago> Pagos { get; } = new();

        public List<string> FiltrosEstado => new() { "Todos", "Pendiente", "Parcial", "Vencido", "Pagado" };
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
                    MontoEditable = value.Monto;
                    MontoPagadoEditable = value.MontoPagado;
                    // Por defecto el monto a abonar es el saldo pendiente (un clic paga todo lo que falta)
                    MontoAbono = value.Saldo;
                    if (value.FechaPago.HasValue) FechaPago = value.FechaPago.Value;
                    FechaVencimientoEditable = value.FechaVencimiento;
                    NotaEditable = value.Nota ?? string.Empty;
                    PagoAdelantadoEditable = value.PagoAdelantado;
                    OnPropertyChanged(nameof(FormaPago));
                    OnPropertyChanged(nameof(Banco));
                }
                OnPropertyChanged(nameof(PeriodoSeleccionadoTexto));
                OnPropertyChanged(nameof(ResumenPagoTexto));
            }
        }

        /// <summary>Texto que indica el periodo seleccionado, ej. "Semana del 1-7 Sep 2026" o "Mes de Septiembre 2026".</summary>
        public string PeriodoSeleccionadoTexto => _seleccionado == null
            ? "Selecciona un periodo en la tabla de abajo"
            : $"{_seleccionado.EstudianteNombre}  —  {_seleccionado.Etiqueta}";

        /// <summary>Resumen del periodo seleccionado: total, abonado y saldo pendiente.</summary>
        public string ResumenPagoTexto => _seleccionado == null
            ? string.Empty
            : $"Total: RD$ {_seleccionado.Monto:N2}      Abonado: RD$ {_seleccionado.MontoPagado:N2}      Saldo pendiente: RD$ {_seleccionado.Saldo:N2}";

        public string FiltroEstado
        {
            get => _filtroEstado;
            set { if (SetProperty(ref _filtroEstado, value)) AplicarFiltro(); }
        }

        public FormaPago FormaPago { get => _formaPago; set => SetProperty(ref _formaPago, value); }
        public string Banco { get => _banco; set => SetProperty(ref _banco, value); }
        public DateTime FechaPago { get => _fechaPago; set => SetProperty(ref _fechaPago, value); }
        public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }

        /// <summary>Total del periodo (permite corregir el total ingresado por error).</summary>
        public decimal MontoEditable { get => _montoEditable; set => SetProperty(ref _montoEditable, value); }

        /// <summary>Monto que el estudiante paga AHORA (abono total o parcial).</summary>
        public decimal MontoAbono { get => _montoAbono; set => SetProperty(ref _montoAbono, value); }

        /// <summary>Total abonado del periodo (permite corregir lo ya pagado si se ingreso por error).</summary>
        public decimal MontoPagadoEditable { get => _montoPagadoEditable; set => SetProperty(ref _montoPagadoEditable, value); }

        /// <summary>Fecha de vencimiento editable del periodo seleccionado.</summary>
        public DateTime? FechaVencimientoEditable { get => _fechaVencimientoEditable; set => SetProperty(ref _fechaVencimientoEditable, value); }

        /// <summary>Nota/comentario editable del periodo seleccionado.</summary>
        public string NotaEditable { get => _notaEditable; set => SetProperty(ref _notaEditable, value); }

        /// <summary>Indica si el pago fue adelantado.</summary>
        public bool PagoAdelantadoEditable { get => _pagoAdelantadoEditable; set => SetProperty(ref _pagoAdelantadoEditable, value); }

        public RelayCommand RegistrarPagoCommand { get; }
        public RelayCommand GuardarCambiosCommand { get; }
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
                "Parcial" => _todos.Where(p => p.Estado == EstadoPago.Parcial),
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

            if (MontoAbono <= 0)
            {
                MessageBox.Show("El monto a pagar debe ser mayor que cero.", "Pagos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal saldo = Seleccionado.Saldo;
            if (MontoAbono > saldo)
            {
                var conf = MessageBox.Show(
                    $"El monto que intentas registrar (RD$ {MontoAbono:N2}) es mayor que el saldo pendiente (RD$ {saldo:N2}).\n\n" +
                    $"Se registrara solo el saldo pendiente (RD$ {saldo:N2}) y el periodo quedara pagado.\n\nDeseas continuar?",
                    "Abono mayor al saldo", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (conf != MessageBoxResult.Yes) return;
            }

            var resultado = _db.RegistrarAbono(Seleccionado.Id, MontoAbono, FormaPago,
                FormaPago == FormaPago.Transferencia ? Banco : string.Empty, FechaPago);

            if (resultado.Completo)
            {
                Mensaje = resultado.Factura != null
                    ? $"Pago completo (RD$ {resultado.TotalPagado:N2}). Periodo PAGADO. Factura {resultado.Factura.NumeroFactura} generada."
                    : $"Pago completo (RD$ {resultado.TotalPagado:N2}). Periodo PAGADO.";
            }
            else
            {
                Mensaje = $"Registrado un abono de RD$ {resultado.Abono:N2}. " +
                          $"Total abonado: RD$ {resultado.TotalPagado:N2}. Saldo pendiente: RD$ {resultado.Saldo:N2}.";
            }

            Cargar();
        }

        /// <summary>
        /// Guarda cambios sobre un pago ya existente: corrige el monto (si se puso por error),
        /// la forma de pago, el banco y la fecha del pago. Tambien corrige la factura asociada.
        /// </summary>
        private void GuardarCambios()
        {
            if (Seleccionado == null) return;

            if (MontoEditable <= 0)
            {
                MessageBox.Show("El total del periodo debe ser mayor que cero.", "Editar pago",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal abonado = MontoPagadoEditable;
            if (abonado < 0) abonado = 0;
            if (abonado > MontoEditable) abonado = MontoEditable;
            decimal saldo = MontoEditable - abonado;

            var r = MessageBox.Show(
                $"Se actualizara el periodo \"{Seleccionado.Etiqueta}\" de {Seleccionado.EstudianteNombre}:\n\n" +
                $"Total del periodo: RD$ {MontoEditable:N2}\n" +
                $"Total abonado: RD$ {abonado:N2}\n" +
                $"Saldo pendiente: RD$ {saldo:N2}\n" +
                $"Forma de pago: {FormaPago}\n" +
                (FormaPago == FormaPago.Transferencia ? $"Banco: {Banco}\n" : string.Empty) +
                $"Fecha de pago: {FechaPago:dd/MM/yyyy}\n\n" +
                "Deseas guardar estos cambios?",
                "Editar pago", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _db.ActualizarPago(Seleccionado.Id, MontoEditable, abonado, FormaPago,
                FormaPago == FormaPago.Transferencia ? Banco : string.Empty, FechaPago,
                FechaVencimientoEditable, PagoAdelantadoEditable, NotaEditable);

            Mensaje = $"Cambios guardados. Total: RD$ {MontoEditable:N2}, abonado: RD$ {abonado:N2}, saldo: RD$ {saldo:N2}.";
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
