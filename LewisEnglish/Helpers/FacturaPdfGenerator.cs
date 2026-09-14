using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string MarcaAgua = "#EEF9F8";
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
        /// Genera el PDF de una factura en la ruta indicada. Compatibilidad: sin abonos.
        /// </summary>
        public static void Generar(Factura factura, string rutaSalida)
            => Generar(factura, null, rutaSalida);

        /// <summary>
        /// Genera el PDF de una factura, incluyendo el proceso de pago (lista de abonos).
        /// </summary>
        public static void Generar(Factura factura, List<Abono>? abonos, string rutaSalida)
        {
            var logo = CargarLogo();
            abonos ??= new List<Abono>();
            decimal totalAbonado = abonos.Count > 0 ? abonos.Sum(a => a.Monto) : factura.Monto;
            if (totalAbonado <= 0) totalAbonado = factura.Monto;
            decimal saldoFinal = factura.Monto - totalAbonado;
            if (saldoFinal < 0) saldoFinal = 0;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(GrisTexto));

                    // Marca de agua (fondo, muy claro para no tapar el texto)
                    page.Background().AlignCenter().AlignMiddle()
                        .Text(saldoFinal > 0 ? "ABONO" : "PAGADO").FontSize(90).Bold().FontColor(MarcaAgua);

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

                        // Detalle del concepto
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

                        // ---------- Proceso de pago (abonos) ----------
                        if (abonos.Count > 0)
                        {
                            col.Item().PaddingTop(6).Text("Proceso de pago")
                                .Bold().FontColor(Navy).FontSize(11);

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);   // Fecha
                                    c.RelativeColumn(3);   // Forma de pago
                                    c.RelativeColumn(2);   // Abono
                                    c.RelativeColumn(2);   // Saldo restante
                                });

                                t.Header(h =>
                                {
                                    h.Cell().Background(Navy).Padding(5)
                                        .Text("Fecha").FontColor("#FFFFFF").Bold().FontSize(9);
                                    h.Cell().Background(Navy).Padding(5)
                                        .Text("Forma de pago").FontColor("#FFFFFF").Bold().FontSize(9);
                                    h.Cell().Background(Navy).Padding(5).AlignRight()
                                        .Text("Abono").FontColor("#FFFFFF").Bold().FontSize(9);
                                    h.Cell().Background(Navy).Padding(5).AlignRight()
                                        .Text("Saldo restante").FontColor("#FFFFFF").Bold().FontSize(9);
                                });

                                int i = 0;
                                foreach (var ab in abonos)
                                {
                                    string fondo = (i % 2 == 0) ? GrisSuave : "#FFFFFF";
                                    i++;
                                    t.Cell().Background(fondo).Padding(5)
                                        .Text($"{ab.Fecha:dd/MM/yyyy}").FontSize(9);
                                    t.Cell().Background(fondo).Padding(5)
                                        .Text(ab.FormaPagoTexto).FontSize(9);
                                    t.Cell().Background(fondo).Padding(5).AlignRight()
                                        .Text($"RD$ {ab.Monto:N2}").FontSize(9);
                                    t.Cell().Background(fondo).Padding(5).AlignRight()
                                        .Text($"RD$ {ab.SaldoDespues:N2}").FontSize(9);
                                }
                            });
                        }

                        // ---------- Resumen de montos ----------
                        col.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            t.Cell().Background(GrisSuave).Padding(6)
                                .Text("Total del periodo").FontColor(Navy);
                            t.Cell().Background(GrisSuave).Padding(6).AlignRight()
                                .Text($"RD$ {factura.Monto:N2}").FontColor(Navy);

                            t.Cell().Background(GrisSuave).Padding(6)
                                .Text("Total abonado").FontColor(Navy);
                            t.Cell().Background(GrisSuave).Padding(6).AlignRight()
                                .Text($"RD$ {totalAbonado:N2}").FontColor(Navy).Bold();

                            t.Cell().Background(GrisSuave).Padding(6)
                                .Text("Saldo pendiente").FontColor(Navy);
                            t.Cell().Background(GrisSuave).Padding(6).AlignRight()
                                .Text($"RD$ {saldoFinal:N2}")
                                .FontColor(saldoFinal > 0 ? "#C0392B" : Turquesa).Bold();
                        });

                        // Total abonado destacado
                        col.Item().PaddingTop(4).AlignRight().Background(Turquesa).Padding(8)
                            .Text($"TOTAL ABONADO: RD$ {totalAbonado:N2}")
                            .FontColor("#FFFFFF").Bold().FontSize(13);

                        // Forma de pago
                        col.Item().PaddingTop(6).Border(1).BorderColor(GrisSuave).Padding(8).Column(c =>
                        {
                            c.Item().Text("Detalle del pago").Bold().FontColor(Navy);
                            c.Item().Text($"Fecha de pago: {factura.Fecha:dd/MM/yyyy}");
                            c.Item().Text($"Forma de pago: {factura.FormaPagoTexto}");
                            if (factura.FormaPago == FormaPago.Transferencia && !string.IsNullOrWhiteSpace(factura.Banco))
                                c.Item().Text($"Banco: {factura.Banco}");
                            c.Item().Text(t =>
                            {
                                t.Span("Estado: ");
                                if (saldoFinal > 0)
                                    t.Span($"ABONO PARCIAL - Falta RD$ {saldoFinal:N2}").Bold().FontColor("#C0392B");
                                else
                                    t.Span("PAGADO").Bold().FontColor(Turquesa);
                            });
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
