using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LewisEnglish.Models;

namespace LewisEnglish.Helpers
{
    /// <summary>
    /// Genera y abre enlaces de WhatsApp para enviar recordatorios de pago.
    /// </summary>
    public static class WhatsAppHelper
    {
        /// <summary>
        /// Construye el mensaje de recordatorio para un estudiante.
        /// </summary>
        public static string ConstruirMensaje(Estudiante estudiante, decimal montoPendiente)
        {
            string frecuencia = estudiante.Frecuencia switch
            {
                FrecuenciaPago.Semanal => "semanal",
                FrecuenciaPago.Quincenal => "quincenal",
                _ => "mensual"
            };
            return $"Hola {estudiante.Nombre}, te recordamos que tu pago {frecuencia} " +
                   $"por RD$ {montoPendiente:N2} esta pendiente. Gracias - Lewis English Speaking Coach";
        }

        /// <summary>
        /// Normaliza un numero telefonico dominicano al formato wa.me (1 + 10 digitos).
        /// </summary>
        public static string NormalizarTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return string.Empty;
            var digitos = new string(telefono.Where(char.IsDigit).ToArray());

            // Quitar el prefijo 1 si ya viene incluido para re-agregarlo de forma uniforme
            if (digitos.Length == 11 && digitos.StartsWith("1"))
                digitos = digitos.Substring(1);

            if (digitos.Length == 10)
                return "1" + digitos;

            // Si ya trae codigo de pais u otro formato, se usa tal cual
            return digitos;
        }

        /// <summary>
        /// Construye la URL wa.me con el mensaje codificado.
        /// </summary>
        public static string ConstruirUrl(string telefono, string mensaje)
        {
            string numero = NormalizarTelefono(telefono);
            string texto = Uri.EscapeDataString(mensaje);
            return $"https://wa.me/{numero}?text={texto}";
        }

        /// <summary>
        /// Abre WhatsApp (web/app) con el mensaje listo para enviar.
        /// </summary>
        public static void Enviar(string telefono, string mensaje)
        {
            string url = ConstruirUrl(telefono, mensaje);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
