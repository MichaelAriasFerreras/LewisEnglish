using System.Windows;
using System.Windows.Input;
using LewisEnglish.ViewModels;

namespace LewisEnglish.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void Ingresar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
                vm.IngresarCommand.Execute(PasswordBox.Password);
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
                vm.IngresarCommand.Execute(PasswordBox.Password);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
