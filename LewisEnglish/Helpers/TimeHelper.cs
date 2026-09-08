using System;
using System.Collections.Generic;
using System.Globalization;

namespace LewisEnglish.Helpers
{
    /// <summary>
    /// Utilidades para el manejo de horarios de clase (formato 12 horas AM/PM).
    /// </summary>
    public static class TimeHelper
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("en-US");
        private static readonly string[] Formatos = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };

        /// <summary>
        /// Calcula la hora de fin sumando 2 horas a la hora de inicio.
        /// Devuelve cadena vacia si no se puede interpretar.
        /// </summary>
        public static string CalcularHoraFin(string horaInicio, double duracionHoras = 2)
        {
            if (TryParse(horaInicio, out var inicio))
            {
                var fin = inicio.AddHours(duracionHoras);
                return fin.ToString("h:mm tt", Cultura);
            }
            return string.Empty;
        }

        public static bool TryParse(string? hora, out DateTime resultado)
        {
            resultado = default;
            if (string.IsNullOrWhiteSpace(hora)) return false;
            return DateTime.TryParseExact(hora.Trim().ToUpperInvariant().Replace(".", ""),
                Formatos, Cultura, DateTimeStyles.None, out resultado)
                || DateTime.TryParse(hora, Cultura, DateTimeStyles.None, out resultado);
        }

        /// <summary>Minutos desde medianoche para poder ordenar/comparar horarios.</summary>
        public static int Minutos(string? hora)
        {
            return TryParse(hora, out var dt) ? dt.Hour * 60 + dt.Minute : int.MaxValue;
        }

        /// <summary>
        /// Lista de franjas horarias disponibles (6:00 AM a 8:00 PM cada 30 min).
        /// </summary>
        public static List<string> FranjasHorarias()
        {
            var lista = new List<string>();
            var inicio = new DateTime(2000, 1, 1, 6, 0, 0);
            var fin = new DateTime(2000, 1, 1, 20, 0, 0);
            while (inicio <= fin)
            {
                lista.Add(inicio.ToString("h:mm tt", Cultura));
                inicio = inicio.AddMinutes(30);
            }
            return lista;
        }
    }
}
