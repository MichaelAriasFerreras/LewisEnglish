using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LewisEnglish.Data;
using LewisEnglish.Helpers;
using LewisEnglish.Models;

namespace LewisEnglish.Services
{
    /// <summary>Metricas para el panel principal (dashboard).</summary>
    public class DashboardMetrics
    {
        public int EstudiantesActivos { get; set; }
        public decimal CobradoMes { get; set; }
        public decimal PendienteMes { get; set; }
        public decimal EsperadoMes { get; set; }
        public int EstudiantesEnMora { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public List<IngresoMensual> IngresosPorMes { get; set; } = new();
        public List<Estudiante> ProximasClases { get; set; } = new();
        public List<Pago> PagosVencidos { get; set; } = new();
    }

    public class IngresoMensual
    {
        public string Mes { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    /// <summary>Resultado de registrar un abono (pago total o parcial) sobre un periodo.</summary>
    public class ResultadoAbono
    {
        /// <summary>Factura generada (solo cuando el periodo queda totalmente pagado).</summary>
        public Factura? Factura { get; set; }
        /// <summary>True si con este abono el periodo quedo totalmente pagado.</summary>
        public bool Completo { get; set; }
        /// <summary>Monto abonado en esta operacion.</summary>
        public decimal Abono { get; set; }
        /// <summary>Total acumulado pagado del periodo despues del abono.</summary>
        public decimal TotalPagado { get; set; }
        /// <summary>Saldo pendiente del periodo despues del abono.</summary>
        public decimal Saldo { get; set; }
    }

    /// <summary>
    /// Capa de acceso a datos y logica de negocio principal.
    /// </summary>
    public class DatabaseService
    {
        // ===================== INICIALIZACION =====================

        public void InicializarBaseDeDatos()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            AplicarActualizacionesEsquema(db);

            if (!db.Admins.Any())
            {
                db.Admins.Add(new Admin
                {
                    Email = "lewis@ingles.com",
                    Password = "lewis123",
                    Nombre = "Lewis"
                });
                db.SaveChanges();
            }

            if (!db.Configuraciones.Any())
            {
                db.Configuraciones.Add(new Configuracion());
                db.SaveChanges();
            }
        }

        /// <summary>Crea tablas nuevas si la base proviene de una version anterior. Idempotente.</summary>
        private void AplicarActualizacionesEsquema(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Estudiantes"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Estudiantes"" PRIMARY KEY AUTOINCREMENT,
                    ""Nombre"" TEXT NOT NULL,
                    ""Telefono"" TEXT NOT NULL,
                    ""WhatsApp"" TEXT NOT NULL,
                    ""Direccion"" TEXT NOT NULL,
                    ""Email"" TEXT NOT NULL,
                    ""DiaClase"" INTEGER NOT NULL,
                    ""HoraInicio"" TEXT NOT NULL,
                    ""Frecuencia"" INTEGER NOT NULL,
                    ""Tarifa"" decimal(18,2) NOT NULL,
                    ""FormaPago"" INTEGER NOT NULL,
                    ""Banco"" TEXT NOT NULL,
                    ""Estado"" INTEGER NOT NULL,
                    ""FechaRegistro"" TEXT NOT NULL,
                    ""DuracionHoras"" REAL NOT NULL DEFAULT 2.0,
                    ""DiaPago"" INTEGER NOT NULL DEFAULT 1
                );");

            // Migracion: agregar columna DuracionHoras a bases de datos existentes
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Estudiantes"" ADD COLUMN ""DuracionHoras"" REAL NOT NULL DEFAULT 2.0");
            // v2.0.0: dia del mes en que el estudiante paga
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Estudiantes"" ADD COLUMN ""DiaPago"" INTEGER NOT NULL DEFAULT 1");

            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Asistencias"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Asistencias"" PRIMARY KEY AUTOINCREMENT,
                    ""EstudianteId"" INTEGER NOT NULL,
                    ""EstudianteNombre"" TEXT NOT NULL,
                    ""Fecha"" TEXT NOT NULL,
                    ""Estado"" INTEGER NOT NULL,
                    ""Nota"" TEXT NOT NULL
                );");

            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Pagos"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pagos"" PRIMARY KEY AUTOINCREMENT,
                    ""EstudianteId"" INTEGER NOT NULL,
                    ""EstudianteNombre"" TEXT NOT NULL,
                    ""Frecuencia"" INTEGER NOT NULL,
                    ""PeriodoInicio"" TEXT NOT NULL,
                    ""PeriodoFin"" TEXT NOT NULL,
                    ""Etiqueta"" TEXT NOT NULL,
                    ""Monto"" decimal(18,2) NOT NULL,
                    ""MontoPagado"" decimal(18,2) NOT NULL DEFAULT 0,
                    ""Estado"" INTEGER NOT NULL,
                    ""FechaPago"" TEXT NULL,
                    ""FormaPago"" INTEGER NOT NULL,
                    ""Banco"" TEXT NOT NULL,
                    ""FacturaGenerada"" INTEGER NOT NULL DEFAULT 0
                );");

            // Migracion: agregar columna MontoPagado a bases de datos existentes
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Pagos"" ADD COLUMN ""MontoPagado"" decimal(18,2) NOT NULL DEFAULT 0");
            // Backfill: los periodos ya marcados como Pagados (Estado=1) deben tener el total abonado
            EjecutarSqlSilencioso(db, @"UPDATE ""Pagos"" SET ""MontoPagado"" = ""Monto"" WHERE ""Estado"" = 1 AND ""MontoPagado"" = 0");

            // v1.9.0: fecha de vencimiento, pago adelantado y nota por periodo
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Pagos"" ADD COLUMN ""FechaVencimiento"" TEXT NULL");
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Pagos"" ADD COLUMN ""PagoAdelantado"" INTEGER NOT NULL DEFAULT 0");
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Pagos"" ADD COLUMN ""Nota"" TEXT NOT NULL DEFAULT ''");
            // Backfill: asignar FechaVencimiento = PeriodoFin para registros existentes sin fecha
            EjecutarSqlSilencioso(db, @"UPDATE ""Pagos"" SET ""FechaVencimiento"" = ""PeriodoFin"" WHERE ""FechaVencimiento"" IS NULL");

            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Facturas"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Facturas"" PRIMARY KEY AUTOINCREMENT,
                    ""PagoId"" INTEGER NOT NULL,
                    ""EstudianteId"" INTEGER NOT NULL,
                    ""NumeroFactura"" TEXT NOT NULL,
                    ""EstudianteNombre"" TEXT NOT NULL,
                    ""Periodo"" TEXT NOT NULL,
                    ""Frecuencia"" INTEGER NOT NULL,
                    ""Monto"" decimal(18,2) NOT NULL,
                    ""Fecha"" TEXT NOT NULL,
                    ""FormaPago"" INTEGER NOT NULL,
                    ""Banco"" TEXT NOT NULL,
                    ""RutaArchivo"" TEXT NOT NULL
                );");

            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Abonos"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Abonos"" PRIMARY KEY AUTOINCREMENT,
                    ""PagoId"" INTEGER NOT NULL,
                    ""EstudianteId"" INTEGER NOT NULL,
                    ""EstudianteNombre"" TEXT NOT NULL,
                    ""PeriodoEtiqueta"" TEXT NOT NULL,
                    ""Monto"" decimal(18,2) NOT NULL,
                    ""TotalPeriodo"" decimal(18,2) NOT NULL,
                    ""TotalAcumulado"" decimal(18,2) NOT NULL,
                    ""SaldoDespues"" decimal(18,2) NOT NULL,
                    ""FormaPago"" INTEGER NOT NULL,
                    ""Banco"" TEXT NOT NULL,
                    ""Fecha"" TEXT NOT NULL
                );");

            db.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS ""Configuraciones"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Configuraciones"" PRIMARY KEY AUTOINCREMENT,
                    ""NombreNegocio"" TEXT NOT NULL DEFAULT '',
                    ""Telefono"" TEXT NOT NULL DEFAULT '',
                    ""Direccion"" TEXT NOT NULL DEFAULT '',
                    ""MensajeRecordatorio"" TEXT NOT NULL DEFAULT ''
                );");
        }

        private void EjecutarSqlSilencioso(AppDbContext db, string sql)
        {
            try { db.Database.ExecuteSqlRaw(sql); }
            catch { /* ya existe: no hacer nada */ }
        }

        // ===================== AUTENTICACION =====================

        public Admin? ValidarLogin(string email, string password)
        {
            using var db = new AppDbContext();
            return db.Admins.FirstOrDefault(a =>
                a.Email.ToLower() == email.ToLower() && a.Password == password);
        }

        public void CambiarCredenciales(int adminId, string nuevoEmail, string nuevaPassword)
        {
            using var db = new AppDbContext();
            var admin = db.Admins.Find(adminId);
            if (admin != null)
            {
                admin.Email = nuevoEmail;
                admin.Password = nuevaPassword;
                db.SaveChanges();
            }
        }

        // ===================== ESTUDIANTES =====================

        public List<Estudiante> ObtenerEstudiantes()
        {
            using var db = new AppDbContext();
            return db.Estudiantes.OrderBy(e => e.Nombre).ToList();
        }

        public List<Estudiante> ObtenerEstudiantesActivos()
        {
            using var db = new AppDbContext();
            return db.Estudiantes
                .Where(e => e.Estado == EstadoEstudiante.Activo)
                .OrderBy(e => e.Nombre)
                .ToList();
        }

        public Estudiante GuardarEstudiante(Estudiante estudiante)
        {
            using var db = new AppDbContext();
            if (estudiante.Id == 0)
            {
                estudiante.FechaRegistro = DateTime.Now;
                db.Estudiantes.Add(estudiante);
            }
            else
            {
                var existente = db.Estudiantes.Find(estudiante.Id);
                if (existente != null)
                {
                    existente.Nombre = estudiante.Nombre;
                    existente.Telefono = estudiante.Telefono;
                    existente.WhatsApp = estudiante.WhatsApp;
                    existente.Direccion = estudiante.Direccion;
                    existente.Email = estudiante.Email;
                    existente.DiaClase = estudiante.DiaClase;
                    existente.HoraInicio = estudiante.HoraInicio;
                    existente.Frecuencia = estudiante.Frecuencia;
                    existente.Tarifa = estudiante.Tarifa;
                    existente.FormaPago = estudiante.FormaPago;
                    existente.Banco = estudiante.Banco;
                    existente.Estado = estudiante.Estado;
                    existente.DuracionHoras = estudiante.DuracionHoras;
                    existente.DiaPago = estudiante.DiaPago;
                }
            }
            db.SaveChanges();
            return estudiante;
        }

        public void EliminarEstudiante(int id)
        {
            using var db = new AppDbContext();
            var e = db.Estudiantes.Find(id);
            if (e != null)
            {
                db.Estudiantes.Remove(e);
                db.SaveChanges();
            }
        }

        // ===================== ASISTENCIA =====================

        public void RegistrarAsistencia(int estudianteId, DateTime fecha, EstadoAsistencia estado, string nota)
        {
            using var db = new AppDbContext();
            var est = db.Estudiantes.Find(estudianteId);
            if (est == null) return;

            // Evita duplicar la asistencia del mismo dia: si existe, la actualiza
            var existente = db.Asistencias.FirstOrDefault(a =>
                a.EstudianteId == estudianteId && a.Fecha.Date == fecha.Date);

            if (existente != null)
            {
                existente.Estado = estado;
                existente.Nota = nota;
            }
            else
            {
                db.Asistencias.Add(new Asistencia
                {
                    EstudianteId = estudianteId,
                    EstudianteNombre = est.Nombre,
                    Fecha = fecha.Date,
                    Estado = estado,
                    Nota = nota
                });
            }
            db.SaveChanges();
        }

        public List<Asistencia> ObtenerAsistenciasPorEstudiante(int estudianteId)
        {
            using var db = new AppDbContext();
            return db.Asistencias
                .Where(a => a.EstudianteId == estudianteId)
                .OrderByDescending(a => a.Fecha)
                .ToList();
        }

        public List<Asistencia> ObtenerAsistencias()
        {
            using var db = new AppDbContext();
            return db.Asistencias.OrderByDescending(a => a.Fecha).Take(200).ToList();
        }

        // ===================== PAGOS / PERIODOS =====================

        /// <summary>
        /// Genera los periodos de pago faltantes para todos los estudiantes activos.
        /// Idempotente: no duplica periodos existentes.
        /// </summary>
        public void GenerarPeriodos()
        {
            using var db = new AppDbContext();
            var hoy = DateTime.Today;
            var activos = db.Estudiantes.Where(e => e.Estado == EstadoEstudiante.Activo).ToList();
            bool cambios = false;

            foreach (var est in activos)
            {
                if (est.Tarifa <= 0) continue;

                var desde = est.FechaRegistro.Date;
                var minimo = hoy.AddMonths(-3);
                if (desde < minimo) desde = minimo;

                var periodos = PeriodoHelper.Generar(est.Frecuencia, desde, hoy);
                var existentes = db.Pagos
                    .Where(p => p.EstudianteId == est.Id)
                    .Select(p => p.PeriodoInicio)
                    .ToList();

                foreach (var per in periodos)
                {
                    if (existentes.Any(e => e.Date == per.Inicio.Date)) continue;

                    var estado = per.Fin.Date < hoy ? EstadoPago.Vencido : EstadoPago.Pendiente;
                    db.Pagos.Add(new Pago
                    {
                        EstudianteId = est.Id,
                        EstudianteNombre = est.Nombre,
                        Frecuencia = est.Frecuencia,
                        PeriodoInicio = per.Inicio,
                        PeriodoFin = per.Fin,
                        Etiqueta = per.Etiqueta,
                        Monto = est.Tarifa,
                        Estado = estado,
                        FormaPago = est.FormaPago,
                        Banco = est.Banco,
                        FacturaGenerada = false,
                        FechaVencimiento = CalcularVencimiento(est.DiaPago, per.Fin)
                    });
                    cambios = true;
                }
            }

            if (cambios) db.SaveChanges();
            ActualizarEstadosVencidos();
        }

        /// <summary>
        /// Calcula la fecha de vencimiento de un periodo usando el dia de pago del
        /// estudiante (1-31), ubicandolo en el mes del fin del periodo. Si el dia
        /// excede los dias del mes, se ajusta al ultimo dia disponible.
        /// </summary>
        public static DateTime CalcularVencimiento(int diaPago, DateTime periodoFin)
        {
            int dia = diaPago < 1 ? 1 : diaPago;
            int anio = periodoFin.Year;
            int mes = periodoFin.Month;
            int diasEnMes = DateTime.DaysInMonth(anio, mes);
            if (dia > diasEnMes) dia = diasEnMes;
            return new DateTime(anio, mes, dia);
        }

        /// <summary>Devuelve los Id de estudiantes con pagos vencidos o parciales (en mora).</summary>
        public HashSet<int> ObtenerIdsEstudiantesMorosos()
        {
            using var db = new AppDbContext();
            return db.Pagos
                .Where(p => p.Estado == EstadoPago.Vencido || p.Estado == EstadoPago.Parcial)
                .Select(p => p.EstudianteId)
                .Distinct()
                .ToHashSet();
        }

        /// <summary>Actualiza solo la fecha de vencimiento de un periodo especifico.</summary>
        public void ActualizarFechaVencimiento(int pagoId, DateTime fechaVencimiento)
        {
            using var db = new AppDbContext();
            var pago = db.Pagos.Find(pagoId);
            if (pago == null) return;
            pago.FechaVencimiento = fechaVencimiento;
            db.SaveChanges();
        }

        /// <summary>
        /// Carga en una sola conexion el detalle de pagos, asistencias y abonos de un
        /// estudiante. Reemplaza tres consultas independientes (mas lentas).
        /// </summary>
        public (List<Pago> Pagos, List<Asistencia> Asistencias, List<Abono> Abonos)
            ObtenerDetalleEstudiante(int estudianteId)
        {
            using var db = new AppDbContext();
            var pagos = db.Pagos
                .Where(p => p.EstudianteId == estudianteId)
                .OrderByDescending(p => p.PeriodoInicio)
                .ToList();
            var asistencias = db.Asistencias
                .Where(a => a.EstudianteId == estudianteId)
                .OrderByDescending(a => a.Fecha)
                .ToList();
            var abonos = db.Abonos
                .Where(a => a.EstudianteId == estudianteId)
                .OrderByDescending(a => a.Fecha)
                .ToList();
            return (pagos, asistencias, abonos);
        }

        /// <summary>Marca como vencidos los periodos pendientes cuya fecha fin ya paso.</summary>
        public void ActualizarEstadosVencidos()
        {
            using var db = new AppDbContext();
            var hoy = DateTime.Today;
            var pendientes = db.Pagos
                .Where(p => p.Estado == EstadoPago.Pendiente && p.PeriodoFin < hoy)
                .ToList();
            if (pendientes.Count > 0)
            {
                foreach (var p in pendientes) p.Estado = EstadoPago.Vencido;
                db.SaveChanges();
            }
        }

        public List<Pago> ObtenerPagos()
        {
            using var db = new AppDbContext();
            return db.Pagos
                .OrderByDescending(p => p.PeriodoInicio)
                .ThenBy(p => p.EstudianteNombre)
                .ToList();
        }

        public List<Pago> ObtenerPagosPorEstudiante(int estudianteId)
        {
            using var db = new AppDbContext();
            return db.Pagos
                .Where(p => p.EstudianteId == estudianteId)
                .OrderByDescending(p => p.PeriodoInicio)
                .ToList();
        }

        public List<Pago> ObtenerPagosPendientes()
        {
            using var db = new AppDbContext();
            return db.Pagos
                .Where(p => p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Vencido || p.Estado == EstadoPago.Parcial)
                .OrderBy(p => p.PeriodoFin)
                .ToList();
        }

        // ===================== ABONOS (movimientos de pago) =====================

        /// <summary>Devuelve los abonos (movimientos) de un periodo, del mas antiguo al mas reciente.</summary>
        public List<Abono> ObtenerAbonosPorPago(int pagoId)
        {
            using var db = new AppDbContext();
            return db.Abonos
                .Where(a => a.PagoId == pagoId)
                .OrderBy(a => a.Fecha)
                .ThenBy(a => a.Id)
                .ToList();
        }

        /// <summary>Devuelve todos los abonos de un estudiante, del mas reciente al mas antiguo.</summary>
        public List<Abono> ObtenerAbonosPorEstudiante(int estudianteId)
        {
            using var db = new AppDbContext();
            return db.Abonos
                .Where(a => a.EstudianteId == estudianteId)
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToList();
        }

        /// <summary>
        /// Registra el pago de un periodo y genera automaticamente su factura (PDF).
        /// </summary>
        public Factura? RegistrarPago(int pagoId, FormaPago formaPago, string banco, DateTime fechaPago)
        {
            using var db = new AppDbContext();
            var pago = db.Pagos.Find(pagoId);
            if (pago == null) return null;

            pago.Estado = EstadoPago.Pagado;
            pago.FechaPago = fechaPago;
            pago.FormaPago = formaPago;
            pago.Banco = banco ?? string.Empty;
            db.SaveChanges();

            return CrearFacturaInterna(db, pago);
        }

        /// <summary>
        /// Registra un ABONO (pago total o parcial) sobre un periodo. Suma el monto
        /// abonado al total ya pagado. Si con este abono se cubre el total del periodo,
        /// el periodo queda "Pagado" y se genera la factura; si aun queda saldo, el
        /// periodo queda "Parcial" y se muestra cuanto falta por pagar.
        /// </summary>
        public ResultadoAbono RegistrarAbono(int pagoId, decimal abono, FormaPago formaPago, string banco, DateTime fechaPago)
        {
            using var db = new AppDbContext();
            var pago = db.Pagos.Find(pagoId);
            if (pago == null) return new ResultadoAbono();

            if (abono <= 0) return new ResultadoAbono { Saldo = pago.Monto - pago.MontoPagado };

            decimal pagadoAntes = pago.MontoPagado;
            pago.MontoPagado += abono;
            if (pago.MontoPagado > pago.Monto) pago.MontoPagado = pago.Monto; // no permitir sobrepago

            // Monto realmente aplicado (por si el abono excedia el saldo)
            decimal abonoAplicado = pago.MontoPagado - pagadoAntes;

            pago.FormaPago = formaPago;
            pago.Banco = formaPago == FormaPago.Transferencia ? (banco ?? string.Empty) : string.Empty;
            pago.FechaPago = fechaPago;

            bool completo = pago.MontoPagado >= pago.Monto && pago.Monto > 0;
            pago.Estado = completo ? EstadoPago.Pagado : EstadoPago.Parcial;

            // Registrar el movimiento (abono) para tener un historial completo
            db.Abonos.Add(new Abono
            {
                PagoId = pago.Id,
                EstudianteId = pago.EstudianteId,
                EstudianteNombre = pago.EstudianteNombre,
                PeriodoEtiqueta = pago.Etiqueta,
                Monto = abonoAplicado,
                TotalPeriodo = pago.Monto,
                TotalAcumulado = pago.MontoPagado,
                SaldoDespues = pago.Monto - pago.MontoPagado,
                FormaPago = pago.FormaPago,
                Banco = pago.Banco,
                Fecha = fechaPago
            });

            db.SaveChanges();

            // Si el periodo quedo completo, abrir automaticamente la fecha de vencimiento
            // del siguiente periodo pendiente del mismo estudiante
            if (completo)
            {
                var siguiente = db.Pagos
                    .Where(p => p.EstudianteId == pago.EstudianteId
                             && p.Id != pago.Id
                             && (p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Vencido || p.Estado == EstadoPago.Parcial))
                    .OrderBy(p => p.PeriodoInicio)
                    .FirstOrDefault();
                if (siguiente != null && !siguiente.FechaVencimiento.HasValue)
                {
                    siguiente.FechaVencimiento = siguiente.PeriodoFin;
                    db.SaveChanges();
                }
            }

            Factura? factura = null;
            // Generar factura solo cuando el periodo queda totalmente pagado (evita facturas "PAGADO" en abonos parciales)
            if (completo && !pago.FacturaGenerada)
                factura = CrearFacturaInterna(db, pago);

            return new ResultadoAbono
            {
                Factura = factura,
                Completo = completo,
                Abono = abonoAplicado,
                TotalPagado = pago.MontoPagado,
                Saldo = pago.Monto - pago.MontoPagado
            };
        }

        /// <summary>
        /// Edita un pago existente (total del periodo, total abonado, forma de pago,
        /// banco y fecha). Recalcula el estado y corrige la factura si existe. Sirve
        /// para corregir montos ingresados por error.
        /// </summary>
        public void ActualizarPago(int pagoId, decimal montoTotal, decimal montoPagado, FormaPago formaPago, string banco, DateTime? fechaPago,
            DateTime? fechaVencimiento = null, bool pagoAdelantado = false, string? nota = null)
        {
            using var db = new AppDbContext();
            var pago = db.Pagos.Find(pagoId);
            if (pago == null) return;

            if (montoTotal > 0) pago.Monto = montoTotal;

            // Ajustar el abonado (no negativo, no mayor que el total)
            if (montoPagado < 0) montoPagado = 0;
            if (montoPagado > pago.Monto) montoPagado = pago.Monto;
            pago.MontoPagado = montoPagado;

            pago.FormaPago = formaPago;
            pago.Banco = formaPago == FormaPago.Transferencia ? (banco ?? string.Empty) : string.Empty;
            if (fechaPago.HasValue) pago.FechaPago = fechaPago;

            // Nuevos campos v1.9.0
            pago.FechaVencimiento = fechaVencimiento;
            pago.PagoAdelantado = pagoAdelantado;
            if (nota != null) pago.Nota = nota;

            // Recalcular estado segun lo abonado
            if (pago.MontoPagado >= pago.Monto && pago.Monto > 0)
                pago.Estado = EstadoPago.Pagado;
            else if (pago.MontoPagado > 0)
                pago.Estado = EstadoPago.Parcial;
            else
                pago.Estado = EstadoPago.Pendiente;

            // Corregir la factura asociada, si existe
            var factura = db.Facturas.FirstOrDefault(f => f.PagoId == pago.Id);
            if (factura != null)
            {
                factura.Monto = pago.Monto;
                factura.FormaPago = pago.FormaPago;
                factura.Banco = pago.Banco;
                if (pago.FechaPago.HasValue) factura.Fecha = pago.FechaPago.Value;

                // Regenerar el PDF de respaldo con los datos corregidos
                try
                {
                    string ruta = FacturaPdfGenerator.RutaRespaldo(factura);
                    var abonos = db.Abonos.Where(a => a.PagoId == pago.Id).OrderBy(a => a.Fecha).ToList();
                    FacturaPdfGenerator.Generar(factura, abonos, ruta);
                    factura.RutaArchivo = ruta;
                }
                catch { /* el respaldo no debe interrumpir la correccion */ }
            }

            db.SaveChanges();
        }

        // ===================== FACTURAS =====================

        private Factura CrearFacturaInterna(AppDbContext db, Pago pago)
        {
            var factura = new Factura
            {
                PagoId = pago.Id,
                EstudianteId = pago.EstudianteId,
                NumeroFactura = $"FAC-{DateTime.Now:yyyyMMdd}-{pago.Id:D4}",
                EstudianteNombre = pago.EstudianteNombre,
                Periodo = pago.Etiqueta,
                Frecuencia = pago.Frecuencia,
                Monto = pago.Monto,
                Fecha = pago.FechaPago ?? DateTime.Now,
                FormaPago = pago.FormaPago,
                Banco = pago.Banco
            };

            db.Facturas.Add(factura);
            pago.FacturaGenerada = true;
            db.SaveChanges();

            // Respaldo automatico del PDF
            try
            {
                string ruta = FacturaPdfGenerator.RutaRespaldo(factura);
                var abonos = db.Abonos.Where(a => a.PagoId == pago.Id).OrderBy(a => a.Fecha).ToList();
                FacturaPdfGenerator.Generar(factura, abonos, ruta);
                factura.RutaArchivo = ruta;
                db.SaveChanges();
            }
            catch { /* el respaldo no debe interrumpir el registro del pago */ }

            return factura;
        }

        public List<Factura> ObtenerFacturas()
        {
            using var db = new AppDbContext();
            return db.Facturas.OrderByDescending(f => f.Fecha).ToList();
        }

        public Factura? ObtenerFacturaPorId(int id)
        {
            using var db = new AppDbContext();
            return db.Facturas.Find(id);
        }

        // ===================== DASHBOARD =====================

        public DashboardMetrics ObtenerMetricasDashboard()
        {
            using var db = new AppDbContext();
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            var m = new DashboardMetrics
            {
                NombreMes = $"{PeriodoHelper.NombreMes(hoy.Month)} {hoy.Year}",
                EstudiantesActivos = db.Estudiantes.Count(e => e.Estado == EstadoEstudiante.Activo)
            };

            // Periodos que se solapan con el mes actual
            var delMes = db.Pagos
                .Where(p => p.PeriodoInicio <= finMes && p.PeriodoFin >= inicioMes)
                .ToList();

            // Cobrado = todo lo abonado (incluye abonos parciales); Pendiente = saldo por cobrar
            m.CobradoMes = delMes.Sum(p => p.MontoPagado);
            m.PendienteMes = delMes.Sum(p => p.Saldo);
            m.EsperadoMes = m.CobradoMes + m.PendienteMes;

            m.PagosVencidos = db.Pagos
                .Where(p => p.Estado == EstadoPago.Vencido)
                .OrderBy(p => p.PeriodoFin)
                .ToList();
            m.EstudiantesEnMora = m.PagosVencidos.Select(p => p.EstudianteId).Distinct().Count();

            // Ingresos de los ultimos 6 meses
            for (int i = 5; i >= 0; i--)
            {
                var mes = inicioMes.AddMonths(-i);
                var mesFin = mes.AddMonths(1).AddDays(-1);
                decimal monto = db.Pagos
                    .Where(p => (p.Estado == EstadoPago.Pagado || p.Estado == EstadoPago.Parcial)
                        && p.FechaPago != null
                        && p.FechaPago >= mes && p.FechaPago <= mesFin)
                    .ToList()
                    .Sum(p => p.MontoPagado);
                m.IngresosPorMes.Add(new IngresoMensual
                {
                    Mes = $"{PeriodoHelper.NombreMes(mes.Month).Substring(0, 3)} {mes.Year}",
                    Monto = monto
                });
            }

            // Proximas clases (estudiantes activos ordenados por dia y hora)
            m.ProximasClases = db.Estudiantes
                .Where(e => e.Estado == EstadoEstudiante.Activo)
                .ToList()
                .OrderBy(e => (int)e.DiaClase)
                .ThenBy(e => TimeHelper.Minutos(e.HoraInicio))
                .Take(10)
                .ToList();

            return m;
        }

        // ===================== CONFIGURACION =====================

        public Configuracion ObtenerConfiguracion()
        {
            using var db = new AppDbContext();
            return db.Configuraciones.OrderBy(c => c.Id).FirstOrDefault() ?? new Configuracion();
        }
    }
}
