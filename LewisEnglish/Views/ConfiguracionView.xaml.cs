using System.Windows;
using System.Windows.Controls;
using LewisEnglish.ViewModels;

namespace LewisEnglish.Views
{
    public partial class ConfiguracionView : UserControl
    {
        public ConfiguracionView()
        {
            InitializeComponent();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfiguracionViewModel vm)
            {
                var datos = new object[] { PassBox.Password, PassConfirmBox.Password };
                if (vm.GuardarCommand.CanExecute(datos))
                    vm.GuardarCommand.Execute(datos);

                PassBox.Password = string.Empty;
                PassConfirmBox.Password = string.Empty;
            }
        }
    }
}
