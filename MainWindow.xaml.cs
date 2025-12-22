using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WassControlSys;

/// <summary>
/// Lógica de interacción para MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Actualizar el icono del botón de maximizar según el estado de la ventana
        this.StateChanged += (s, e) => UpdateMaximizeButtonIcon();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
        UpdateMaximizeButtonIcon();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (Application.Current is App app && app.IsShuttingDown)
        {
            // Permitir el cierre real
            base.OnClosing(e);
        }
        else
        {
            // Cancelar cierre y ocultar
            e.Cancel = true;
            this.Hide();
        }
    }

    private void UpdateMaximizeButtonIcon()
    {
        if (MaximizeButton != null)
        {
            MaximizeButton.Content = this.WindowState == WindowState.Maximized ? "🗗" : "🗖";
        }
    }
}