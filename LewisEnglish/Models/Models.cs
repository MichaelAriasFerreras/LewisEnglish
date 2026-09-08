using System;
using System.Collections.Generic;
using LewisEnglish.Helpers;

namespace LewisEnglish.Models
{
    /// <summary>
    /// Usuario administrador del sistema (el coach).
    /// </summary>
    public class Admin
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombre { get; set; } = "Lewis";
    }

    /// <summary>
    /// Configuracion global del negocio (una sola fila).
    /// </summary>
    public class Configuracion
    {
        public int Id { get; set; }
        public string NombreNegocio { get; set; } = "Lewis, English Speaking Coach";
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string MensajeRecordatorio { get; set; } =
            "Hola {nombre}, te recordamos que tu pago de {frecuencia} por RD$ {monto} esta pendiente. Gracias - Lewis English Speaking Coach";
    }

    /// <summary>Dias de la semana en que se imparten clases.</summary>
    public enum DiaSemana
    {
        Lunes = 0,
        Martes = 1,
        Miercoles = 2,
        Jueves = 3,
        Viernes = 4,
        Sabado = 5,
        Domingo = 6
    }

    /// <summary>Frecuencia con la que el estudiante paga.</summary>
    public enum FrecuenciaPago
    {
        Semanal = 0,
        Quincenal = 1,
        Mensual = 2
    }

    /// <summary>Forma de pago del estudiante.</summary>
    public enum FormaPago
    {
        Efectivo = 0,
        Transferencia = 1
    }

    /// <summary>Estado de asistencia a una clase.</summary>
    public enum EstadoAsistencia
    {
        Presente = 0,
        Ausente = 1,
        Justificado = 2
    }

    /// <summary>Estado de un periodo de pago.</summary>
    public enum EstadoPago
    {
        Pendiente = 0,
        Pagado = 1,
        Vencido = 2
    }

    /// <summary>Estado del estudiante.</summary>
    public enum EstadoEstudiante
    {
        Activo = 0,
        Inactivo = 1
    }

    /// <summary>
    /// Perfil de un estudiante de ingles.
    /// </summary>
    public class Estudiante : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void Notificar([System.Runtime.CompilerServices.CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));

        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>Dia de la semana en que recibe clase.</summary>
        public DiaSemana DiaClase { get; set; } = DiaSemana.Lunes;

        /// <summary>Hora de inicio de la clase, formato "h:mm tt" (ej. "8:00 AM").</summary>
        public string HoraInicio { get; set; } = "8:00 AM";

        /// <summary>Duracion de la clase en horas (1, 1.5, 2, etc.). Por defecto 2.</summary>
        public double DuracionHoras { get; set; } = 2;

        public FrecuenciaPago Frecuencia { get; set; } = FrecuenciaPago.Semanal;

        /// <summary>Tarifa personalizada por periodo de pago.</summary>
        public decimal Tarifa { get; set; }

        private FormaPago _formaPago = FormaPago.Efectivo;
        public FormaPago FormaPago
        {
            get => _formaPago;
            set { _formaPago = value; Notificar(); }
        }

        /// <summary>Banco (aplica cuando la forma de pago es transferencia).</summary>
        public string Banco { get; set; } = string.Empty;

        public EstadoEstudiante Estado { get; set; } = EstadoEstudiante.Activo;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public List<Asistencia> Asistencias { get; set; } = new();
        public List<Pago> Pagos { get; set; } = new();

        // --------- Propiedades calculadas (no persistidas) ---------

        /// <summary>Hora de fin = hora de inicio + duracion personalizada.</summary>
        public string HoraFin => TimeHelper.CalcularHoraFin(HoraInicio, DuracionHoras);

        /// <summary>Rango horario completo, ej. "8:00 AM - 10:00 AM (2h)".</summary>
        public string RangoHorario => string.IsNullOrWhiteSpace(HoraInicio)
            ? "-" : $"{HoraInicio} - {HoraFin} ({DuracionHoras}h)";

        public string DiaClaseTexto => DiaClase.ToString();

        public string FrecuenciaTexto => Frecuencia.ToString();

        public string EstadoTexto => Estado == EstadoEstudiante.Activo ? "Activo" : "Inactivo";

        public string FormaPagoTexto => FormaPago == FormaPago.Transferencia
            ? (string.IsNullOrWhiteSpace(Banco) ? "Transferencia" : $"Transferencia - {Banco}")
            : "Efectivo";
    }

    /// <summary>
    /// Registro de asistencia de un estudiante a una clase.
    /// </summary>
    public class Asistencia
    {
        public int Id { get; set; }

        public int EstudianteId { get; set; }
        public Estudiante? Estudiante { get; set; }

        public string EstudianteNombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public EstadoAsistencia Estado { get; set; } = EstadoAsistencia.Presente;

        public string Nota { get; set; } = string.Empty;

        // --------- Presentacion ---------
        public string EstadoTexto => Estado switch
        {
            EstadoAsistencia.Presente => "Presente",
            EstadoAsistencia.Ausente => "Ausente",
            EstadoAsistencia.Justificado => "Justificado",
            _ => "-"
        };
    }

    /// <summary>
    /// Periodo de pago de un estudiante (semana / quincena / mes).
    /// </summary>
    public class Pago
    {
        public int Id { get; set; }

        public int EstudianteId { get; set; }
        public Estudiante? Estudiante { get; set; }

        public string EstudianteNombre { get; set; } = string.Empty;

        public FrecuenciaPago Frecuencia { get; set; }

        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFin { get; set; }

        /// <summary>Etiqueta legible del periodo, ej. "Semana del 1-7 Sep 2026".</summary>
        public string Etiqueta { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

        public DateTime? FechaPago { get; set; }

        public FormaPago FormaPago { get; set; } = FormaPago.Efectivo;

        public string Banco { get; set; } = string.Empty;

        /// <summary>Indica si ya se genero factura para este periodo pagado.</summary>
        public bool FacturaGenerada { get; set; }

        // --------- Presentacion ---------
        public string EstadoTexto => Estado switch
        {
            EstadoPago.Pagado => "Pagado",
            EstadoPago.Vencido => "Vencido",
            _ => "Pendiente"
        };

        public string FrecuenciaTexto => Frecuencia.ToString();

        public string FormaPagoTexto => FormaPago == FormaPago.Transferencia
            ? (string.IsNullOrWhiteSpace(Banco) ? "Transferencia" : $"Transferencia - {Banco}")
            : "Efectivo";
    }

    /// <summary>
    /// Factura generada cuando un periodo queda pagado.
    /// </summary>
    public class Factura
    {
        public int Id { get; set; }

        public int PagoId { get; set; }
        public int EstudianteId { get; set; }

        public string NumeroFactura { get; set; } = string.Empty;
        public string EstudianteNombre { get; set; } = string.Empty;

        public string Periodo { get; set; } = string.Empty;
        public FrecuenciaPago Frecuencia { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public FormaPago FormaPago { get; set; } = FormaPago.Efectivo;
        public string Banco { get; set; } = string.Empty;

        /// <summary>Ruta del PDF respaldado automaticamente.</summary>
        public string RutaArchivo { get; set; } = string.Empty;

        // --------- Presentacion ---------
        public string FormaPagoTexto => FormaPago == FormaPago.Transferencia
            ? (string.IsNullOrWhiteSpace(Banco) ? "Transferencia" : $"Transferencia - {Banco}")
            : "Efectivo";
    }
}
