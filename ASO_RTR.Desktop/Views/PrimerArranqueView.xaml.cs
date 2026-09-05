using System.Windows;
using ASO_RTR.Desktop.Services;
using ASO_RTR.Desktop.ViewModels;

namespace ASO_RTR.Desktop.Views;

public partial class PrimerArranqueView : Window
{
    public PrimerArranqueViewModel ViewModel { get; } = new();

    public PrimerArranqueView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        EscalaVentana.Aplicar(this);
        UsuarioBox.Focus();
    }

    private void OnCrear(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Crear(PasswordBox.Password))
            DialogResult = true;
    }
}
