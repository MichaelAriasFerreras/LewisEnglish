using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using LewisEnglish.Models;

namespace LewisEnglish.Data
{
    /// <summary>
    /// Contexto de Entity Framework Core sobre SQLite.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
        public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
        public DbSet<Asistencia> Asistencias => Set<Asistencia>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Factura> Facturas => Set<Factura>();

        /// <summary>
        /// Ruta del archivo de base de datos, en la carpeta de datos de la aplicacion.
        /// </summary>
        public static string DbPath
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LewisEnglish");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "lewis.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={DbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estudiante>()
                .HasMany(e => e.Asistencias)
                .WithOne(a => a.Estudiante)
                .HasForeignKey(a => a.EstudianteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Estudiante>()
                .HasMany(e => e.Pagos)
                .WithOne(p => p.Estudiante)
                .HasForeignKey(p => p.EstudianteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Precision para valores monetarios
            modelBuilder.Entity<Estudiante>().Property(e => e.Tarifa).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Pago>().Property(p => p.Monto).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Factura>().Property(f => f.Monto).HasColumnType("decimal(18,2)");

            // Ignorar propiedades calculadas / de presentacion
            modelBuilder.Entity<Estudiante>().Ignore(e => e.HoraFin);
            modelBuilder.Entity<Estudiante>().Ignore(e => e.RangoHorario);
            modelBuilder.Entity<Estudiante>().Ignore(e => e.DiaClaseTexto);
            modelBuilder.Entity<Estudiante>().Ignore(e => e.FrecuenciaTexto);
            modelBuilder.Entity<Estudiante>().Ignore(e => e.EstadoTexto);
            modelBuilder.Entity<Estudiante>().Ignore(e => e.FormaPagoTexto);
            modelBuilder.Entity<Asistencia>().Ignore(a => a.EstadoTexto);
            modelBuilder.Entity<Pago>().Ignore(p => p.EstadoTexto);
            modelBuilder.Entity<Pago>().Ignore(p => p.FrecuenciaTexto);
            modelBuilder.Entity<Pago>().Ignore(p => p.FormaPagoTexto);
            modelBuilder.Entity<Factura>().Ignore(f => f.FormaPagoTexto);

            // Datos por defecto: administrador inicial (el coach)
            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                Id = 1,
                Email = "lewis@ingles.com",
                Password = "lewis123",
                Nombre = "Lewis"
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
