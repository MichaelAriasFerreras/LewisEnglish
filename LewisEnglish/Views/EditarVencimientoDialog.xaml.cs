using System;
using System.Windows;

namespace LewisEnglish.Views
{
    /// <summary>Dialogo simple para editar la fecha de vencimiento de un periodo de pago.</summary>
    public partial class EditarVencimientoDialog : Window
    {
        /// <summary>Fecha elegida por el usuario (null si canceló o no seleccionó).</summary>
        public DateTime? FechaSeleccionada { get; private set; }

        public EditarVencimientoDialog(string periodo, DateTime fechaActual)
        {
            InitializeComponent();
            TxtPeriodo.Text = periodo;
            Picker.SelectedDate = fechaActual;
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (Picker.SelectedDate == null)
            {
                MessageBox.Show("Seleccione una fecha válida.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FechaSeleccionada = Picker.SelectedDate;
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
