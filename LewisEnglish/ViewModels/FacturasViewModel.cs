using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using LewisEnglish.Helpers;
using LewisEnglish.Models;
using LewisEnglish.Services;

namespace LewisEnglish.ViewModels
{
    /// <summary>
    /// Listado de facturas generadas, con opciones para abrir o guardar el PDF.
    /// </summary>
    public class FacturasViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private Factura? _seleccionada;
        private string _mensaje = string.Empty;

        public FacturasViewModel(DatabaseService db)
        {
            _db = db;
            AbrirCommand = new RelayCommand(_ => Abrir(), _ => Seleccionada != null);
            GuardarComoCommand = new RelayCommand(_ => GuardarComo(), _ => Seleccionada != null);
            EnviarWhatsAppCommand = new RelayCommand(_ => EnviarWhatsApp(), _ => Seleccionada != null);
        }

        public ObservableCollection<Factura> Facturas { get; } = new();

        public Factura? Seleccionada
        {
            get => _seleccionada;
            set => SetProperty(ref _seleccionada, value);
        }

        public string Mensaje { get => _mensaje; set => SetProperty(ref _mensaje, value); }

        public RelayCommand AbrirCommand { get; }
        public RelayCommand GuardarComoCommand { get; }
        public RelayCommand EnviarWhatsAppCommand { get; }

        public void Cargar()
        {
            Facturas.Clear();
            foreach (var f in _db.ObtenerFacturas())
                Facturas.Add(f);
            Mensaje = string.Empty;
        }

        /// <summary>Genera el PDF si no existe y devuelve una ruta valida.</summary>
        private string? AsegurarPdf(Factura factura)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(factura.RutaArchivo) || !File.Exists(factura.RutaArchivo))
                {
                    string ruta = FacturaPdfGenerator.RutaRespaldo(factura);
                    FacturaPdfGenerator.Generar(factura, ruta);
                    return ruta;
                }
                return factura.RutaArchivo;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo generar el PDF.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void Abrir()
        {
            if (Seleccionada == null) return;
            var ruta = AsegurarPdf(Seleccionada);
            if (ruta == null) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = ruta, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir la factura.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuardarComo()
        {
            if (Seleccionada == null) return;
            var ruta = AsegurarPdf(Seleccionada);
            if (ruta == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"{Seleccionada.NumeroFactura}.pdf",
                Title = "Guardar factura"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.Copy(ruta, dlg.FileName, true);
                    Mensaje = "Factura guardada correctamente.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo guardar la factura.\n\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Abre el chat de WhatsApp del estudiante con un mensaje listo y copia el PDF de la
        /// factura al portapapeles para que solo haya que pegarlo (Ctrl+V) y enviarlo.
        /// Nota: WhatsApp no permite adjuntar archivos automaticamente desde un enlace, por eso
        /// se copia el PDF y se abre la carpeta para arrastrarlo si se prefiere.
        /// </summary>
        private void EnviarWhatsApp()
        {
            if (Seleccionada == null) return;

            var ruta = AsegurarPdf(Seleccionada);
            if (ruta == null) return;

            // Buscar el telefono/WhatsApp del estudiante de la factura
            var estudiante = _db.ObtenerEstudiantes().FirstOrDefault(e => e.Id == Seleccionada.EstudianteId);
            string telefono = estudiante == null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(estudiante.WhatsApp) ? estudiante.Telefono : estudiante.WhatsApp);

            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show(
                    "El estudiante no tiene numero de telefono/WhatsApp registrado.\n\n" +
                    "Agregalo en su perfil (seccion Estudiantes) y vuelve a intentarlo.",
                    "WhatsApp", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Copiar el PDF al portapapeles para pegarlo directamente en el chat
            bool copiado = false;
            try
            {
                var archivos = new StringCollection { ruta };
                Clipboard.SetFileDropList(archivos);
                copiado = true;
            }
            catch { /* si falla el portapapeles, igual se abre el chat y la carpeta */ }

            // Abrir el Explorador con el PDF seleccionado (para arrastrarlo si se prefiere)
            try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{ruta}\"") { UseShellExecute = true }); }
            catch { /* opcional */ }

            string mensaje =
                $"Hola {Seleccionada.EstudianteNombre}, aqui esta tu factura {Seleccionada.NumeroFactura} " +
                $"por RD$ {Seleccionada.Monto:N2} ({Seleccionada.Periodo}). " +
                "Gracias - Lewis, English Speaking Coach";

            try
            {
                WhatsAppHelper.Enviar(telefono, mensaje);
                Mensaje = copiado
                    ? "Se abrio WhatsApp y la factura (PDF) se copio. En el chat presiona Ctrl+V para adjuntarla y presiona Enviar."
                    : "Se abrio WhatsApp. Arrastra el PDF (se abrio su carpeta) al chat y presiona Enviar.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir WhatsApp.\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
