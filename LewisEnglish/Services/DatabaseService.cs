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
                    ""DuracionHoras"" REAL NOT NULL DEFAULT 2.0
                );");

            // Migracion: agregar columna DuracionHoras a bases de datos existentes
            EjecutarSqlSilencioso(db, @"ALTER TABLE ""Estudiantes"" ADD COLUMN ""DuracionHoras"" REAL NOT NULL DEFAULT 2.0");

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
                    ""Estado"" INTEGER NOT NULL,
                    ""FechaPago"" TEXT NULL,
                    ""FormaPago"" INTEGER NOT NULL,
                    ""Banco"" TEXT NOT NULL,
                    ""FacturaGenerada"" INTEGER NOT NULL DEFAULT 0
                );");

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
                        FacturaGenerada = false
                    });
                    cambios = true;
                }
            }

            if (cambios) db.SaveChanges();
            ActualizarEstadosVencidos();
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
                .Where(p => p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Vencido)
                .OrderBy(p => p.PeriodoFin)
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
                FacturaPdfGenerator.Generar(factura, ruta);
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

            m.CobradoMes = delMes.Where(p => p.Estado == EstadoPago.Pagado).Sum(p => p.Monto);
            m.PendienteMes = delMes.Where(p => p.Estado != EstadoPago.Pagado).Sum(p => p.Monto);
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
                    .Where(p => p.Estado == EstadoPago.Pagado
                        && p.FechaPago != null
                        && p.FechaPago >= mes && p.FechaPago <= mesFin)
                    .ToList()
                    .Sum(p => p.Monto);
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
