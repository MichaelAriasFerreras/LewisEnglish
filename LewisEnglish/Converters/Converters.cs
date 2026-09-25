using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LewisEnglish.Models;

namespace LewisEnglish.Converters
{
    /// <summary>Formatea un decimal como moneda dominicana: RD$ 1,234.56</summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d) return $"RD$ {d:N2}";
            if (value is double db) return $"RD$ {db:N2}";
            if (value is int i) return $"RD$ {i:N2}";
            return "RD$ 0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Formatea una fecha como dd/MM/yyyy.</summary>
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt) return dt.ToString("dd/MM/yyyy");
            if (value is DateTime?)
            {
                var n = (DateTime?)value;
                return n.HasValue ? n.Value.ToString("dd/MM/yyyy") : "-";
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Color de fondo segun el estado de un periodo de pago.</summary>
    public class EstadoPagoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoPago estado)
            {
                return estado switch
                {
                    EstadoPago.Pagado => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20")),
                    EstadoPago.Vencido => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                    EstadoPago.Parcial => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2760C")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7791F"))
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Color de fondo segun el estado de asistencia.</summary>
    public class EstadoAsistenciaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoAsistencia estado)
            {
                return estado switch
                {
                    EstadoAsistencia.Presente => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20")),
                    EstadoAsistencia.Ausente => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7791F"))
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Color segun el estado del estudiante (activo / inactivo).</summary>
    public class EstadoEstudianteColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstadoEstudiante estado)
            {
                return estado == EstadoEstudiante.Activo
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563"));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>
    /// Color para la fecha de vencimiento de un periodo (recibe el objeto Pago):
    /// verde = pagado, rojo = vencido/no pagado, naranja = vence pronto (3 dias), gris = normal.
    /// </summary>
    public class VencimientoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Pago pago)
            {
                if (pago.Estado == EstadoPago.Pagado)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")); // verde

                var vence = pago.FechaVencimiento ?? pago.PeriodoFin;
                var hoy = DateTime.Today;
                if (vence.Date < hoy)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53E3E")); // rojo (vencido)
                if (vence.Date <= hoy.AddDays(3))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5A623")); // naranja (vence pronto)
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8FB0C4")); // gris (normal)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Convierte booleano en Visibility (parametro "invert" para invertir).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            if (parameter is string s && s == "invert") b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Compara el valor con el parametro y devuelve true si son iguales (sidebar activo).</summary>
    public class SeccionActivaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seccion = value?.ToString() ?? string.Empty;
            var esperado = parameter?.ToString() ?? string.Empty;
            return seccion == esperado;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
