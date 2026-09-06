using System;
using System.IO;
using LewisEnglish.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LewisEnglish.Helpers
{
    /// <summary>
    /// Genera facturas PDF profesionales con la identidad de "Lewis, English Speaking Coach".
    /// Usa QuestPDF (licencia Community).
    /// </summary>
    public static class FacturaPdfGenerator
    {
        // Paleta corporativa Lewis
        private const string Navy = "#1A3A52";
        private const string Turquesa = "#00A9A5";
        private const string TurquesaSuave = "#D6F0EF";
        private const string GrisTexto = "#3A3A3A";
        private const string GrisSuave = "#EEF2F7";

        static FacturaPdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>Carpeta de respaldo automatico de facturas.</summary>
        public static string CarpetaRespaldo()
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LewisEnglish", "Facturas");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        /// <summary>Ruta de respaldo sugerida para una factura.</summary>
        public static string RutaRespaldo(Factura factura)
        {
            string nombre = $"{factura.NumeroFactura}_{Limpiar(factura.EstudianteNombre)}.pdf";
            return Path.Combine(CarpetaRespaldo(), nombre);
        }

        private static string Limpiar(string texto)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                texto = texto.Replace(c, '_');
            return texto.Replace(' ', '_');
        }

        private static byte[]? CargarLogo()
        {
            try
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "lewis_logo.png");
                if (File.Exists(ruta))
                    return File.ReadAllBytes(ruta);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Genera el PDF de una factura en la ruta indicada.
        /// </summary>
        public static void Generar(Factura factura, string rutaSalida)
        {
            var logo = CargarLogo();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(GrisTexto));

                    // Marca de agua "PAGADO"
                    page.Foreground().AlignCenter().AlignMiddle()
                        .Text("PAGADO").FontSize(90).Bold().FontColor(TurquesaSuave);

                    // ---------- Encabezado ----------
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logo != null)
                                row.RelativeItem().Height(46).AlignLeft().Image(logo).FitHeight();
                            else
                                row.RelativeItem().Text("Lewis, English Speaking Coach")
                                    .Bold().FontSize(15).FontColor(Navy);

                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text("FACTURA").Bold().FontSize(18).FontColor(Navy);
                                c.Item().Text(factura.NumeroFactura).FontSize(11).FontColor(Turquesa);
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(2).LineColor(Turquesa);
                    });

                    // ---------- Contenido ----------
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Facturado a").Bold().FontColor(Navy);
                                c.Item().Text(factura.EstudianteNombre).FontSize(12).Bold();
                            });
                            r.ConstantItem(170).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Fecha: {factura.Fecha:dd/MM/yyyy}");
                                c.Item().Text($"Frecuencia: {factura.Frecuencia}");
                            });
                        });

                        // Detalle
                        col.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background(Navy).Padding(6)
                                    .Text("Concepto").FontColor("#FFFFFF").Bold();
                                h.Cell().Background(Navy).Padding(6).AlignRight()
                                    .Text("Monto").FontColor("#FFFFFF").Bold();
                            });

                            t.Cell().Background(GrisSuave).Padding(6)
                                .Text($"Clases de ingles - {factura.Periodo}");
                            t.Cell().Background(GrisSuave).Padding(6).AlignRight()
                                .Text($"RD$ {factura.Monto:N2}");
                        });

                        // Total
                        col.Item().PaddingTop(4).AlignRight().Background(Turquesa).Padding(8)
                            .Text($"TOTAL PAGADO: RD$ {factura.Monto:N2}")
                            .FontColor("#FFFFFF").Bold().FontSize(13);

                        // Forma de pago
                        col.Item().PaddingTop(6).Border(1).BorderColor(GrisSuave).Padding(8).Column(c =>
                        {
                            c.Item().Text("Detalle del pago").Bold().FontColor(Navy);
                            c.Item().Text($"Forma de pago: {factura.FormaPagoTexto}");
                            c.Item().Text($"Estado: PAGADO");
                        });
                    });

                    // ---------- Pie ----------
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(GrisSuave);
                        col.Item().PaddingTop(4).AlignCenter()
                            .Text("Lewis, English Speaking Coach")
                            .FontSize(9).Bold().FontColor(Navy);
                        col.Item().AlignCenter()
                            .Text("Gracias por confiar en nuestras clases de ingles.")
                            .FontSize(8).Italic().FontColor(GrisTexto);
                    });
                });
            }).GeneratePdf(rutaSalida);
        }
    }
}
