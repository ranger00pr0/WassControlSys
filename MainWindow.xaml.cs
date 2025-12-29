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
using WassControlSys.ViewModels;

namespace WassControlSys;

/// <summary>
/// Lógica de interacción para MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Forzar estado normal ANTES de que se muestre
        this.WindowState = WindowState.Normal;
        
        // Actualizar el icono del botón de maximizar según el estado de la ventana
        this.StateChanged += (s, e) => UpdateMaximizeButtonIcon();

        this.IsVisibleChanged += (s, e) => 
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.IsWindowVisible = this.IsVisible;
            }
        };

        // SourceInitialized se ejecuta ANTES de que la ventana se muestre
        this.SourceInitialized += (s, e) =>
        {
            this.WindowState = WindowState.Normal;
            this.Width = 1000;
            this.Height = 600;
        };

        // Forzar ventana al frente al cargar
        this.Loaded += (s, e) =>
        {
            this.WindowState = WindowState.Normal; // Asegurar estado normal
            this.Activate();
            this.Focus();
            this.Topmost = true;
            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => this.Topmost = false);
            });
        };
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
            base.OnClosing(e);
            return;
        }

        bool minimize = true; // Default
        if (this.DataContext is MainViewModel vm)
        {
            minimize = vm.MinimizeToTray;
        }

        if (minimize)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            // Apagado real si la opción está desactivada
            if (Application.Current is App app2) app2.Shutdown();
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