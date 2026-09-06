using System;
using System.Collections.Generic;
using System.Globalization;
using LewisEnglish.Models;

namespace LewisEnglish.Helpers
{
    /// <summary>
    /// Representa un periodo de pago calculado (rango + etiqueta legible).
    /// </summary>
    public struct PeriodoInfo
    {
        public DateTime Inicio;
        public DateTime Fin;
        public string Etiqueta;
    }

    /// <summary>
    /// Genera los periodos de pago (semana / quincena / mes) segun la frecuencia.
    /// </summary>
    public static class PeriodoHelper
    {
        private static readonly string[] MesesCortos =
            { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        private static readonly string[] MesesLargos =
            { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio",
              "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

        /// <summary>
        /// Genera todos los periodos de una frecuencia que se solapan con el rango [desde, hasta].
        /// </summary>
        public static List<PeriodoInfo> Generar(FrecuenciaPago frecuencia, DateTime desde, DateTime hasta)
        {
            var lista = new List<PeriodoInfo>();
            desde = desde.Date;
            hasta = hasta.Date;

            switch (frecuencia)
            {
                case FrecuenciaPago.Semanal:
                    GenerarSemanal(lista, desde, hasta);
                    break;
                case FrecuenciaPago.Quincenal:
                    GenerarQuincenal(lista, desde, hasta);
                    break;
                default:
                    GenerarMensual(lista, desde, hasta);
                    break;
            }
            return lista;
        }

        private static void GenerarSemanal(List<PeriodoInfo> lista, DateTime desde, DateTime hasta)
        {
            // La semana empieza el lunes
            var inicio = InicioSemana(desde);
            while (inicio <= hasta)
            {
                var fin = inicio.AddDays(6);
                lista.Add(new PeriodoInfo
                {
                    Inicio = inicio,
                    Fin = fin,
                    Etiqueta = EtiquetaSemana(inicio, fin)
                });
                inicio = inicio.AddDays(7);
            }
        }

        private static void GenerarQuincenal(List<PeriodoInfo> lista, DateTime desde, DateTime hasta)
        {
            var mes = new DateTime(desde.Year, desde.Month, 1);
            while (mes <= hasta)
            {
                int dias = DateTime.DaysInMonth(mes.Year, mes.Month);

                var q1Ini = new DateTime(mes.Year, mes.Month, 1);
                var q1Fin = new DateTime(mes.Year, mes.Month, 15);
                if (q1Fin >= desde && q1Ini <= hasta)
                    lista.Add(new PeriodoInfo
                    {
                        Inicio = q1Ini,
                        Fin = q1Fin,
                        Etiqueta = $"Quincena 1-15 {MesesCortos[mes.Month - 1]} {mes.Year}"
                    });

                var q2Ini = new DateTime(mes.Year, mes.Month, 16);
                var q2Fin = new DateTime(mes.Year, mes.Month, dias);
                if (q2Fin >= desde && q2Ini <= hasta)
                    lista.Add(new PeriodoInfo
                    {
                        Inicio = q2Ini,
                        Fin = q2Fin,
                        Etiqueta = $"Quincena 16-{dias} {MesesCortos[mes.Month - 1]} {mes.Year}"
                    });

                mes = mes.AddMonths(1);
            }
        }

        private static void GenerarMensual(List<PeriodoInfo> lista, DateTime desde, DateTime hasta)
        {
            var mes = new DateTime(desde.Year, desde.Month, 1);
            while (mes <= hasta)
            {
                int dias = DateTime.DaysInMonth(mes.Year, mes.Month);
                lista.Add(new PeriodoInfo
                {
                    Inicio = mes,
                    Fin = new DateTime(mes.Year, mes.Month, dias),
                    Etiqueta = $"Mes de {MesesLargos[mes.Month - 1]} {mes.Year}"
                });
                mes = mes.AddMonths(1);
            }
        }

        private static DateTime InicioSemana(DateTime fecha)
        {
            int diff = ((int)fecha.DayOfWeek + 6) % 7; // lunes = 0
            return fecha.AddDays(-diff).Date;
        }

        private static string EtiquetaSemana(DateTime inicio, DateTime fin)
        {
            if (inicio.Month == fin.Month)
                return $"Semana del {inicio.Day}-{fin.Day} {MesesCortos[inicio.Month - 1]} {fin.Year}";
            return $"Semana del {inicio.Day} {MesesCortos[inicio.Month - 1]} - {fin.Day} {MesesCortos[fin.Month - 1]} {fin.Year}";
        }

        public static string NombreMes(int mes) =>
            (mes >= 1 && mes <= 12) ? MesesLargos[mes - 1] : "";
    }
}
