using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    }
}
