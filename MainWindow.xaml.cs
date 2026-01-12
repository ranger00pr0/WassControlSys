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
using WassControlSys.Core;

namespace WassControlSys;

/// <summary>
/// Lógica de interacción para MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogService _logService;
    public MainWindow(ILogService logService)
    {
        _logService = logService;
        InitializeComponent();
        
        _logService.Info($"MainWindow constructor - Initial WindowState: {this.WindowState}");

        // Forzar estado normal ANTES de que se muestre
        this.WindowState = WindowState.Normal;
        _logService.Info($"MainWindow constructor - WindowState set to Normal: {this.WindowState}");
        
        // Actualizar el icono del botón de maximizar según el estado de la ventana



        void UpdateVMVisibility()
        {
            if (this.DataContext is MainViewModel vm)
            {
                // App is "Visible" only if Window is Visible AND Not Minimized
                vm.IsWindowVisible = this.IsVisible && this.WindowState != WindowState.Minimized;
            }
             _logService.Info($"Window State Changed - Visible: {this.IsVisible}, State: {this.WindowState}");
        }

        this.IsVisibleChanged += (s, e) => UpdateVMVisibility();
        this.StateChanged += (s, e) => UpdateVMVisibility();


        // SourceInitialized se ejecuta ANTES de que la ventana se muestre
        this.SourceInitialized += (s, e) =>
        {
            this.WindowState = WindowState.Normal;
            this.Width = 1000;
            this.Height = 600;
            _logService.Info($"MainWindow SourceInitialized event - WindowState set to Normal, Width=1000, Height=600. Current WindowState: {this.WindowState}");
        };

        // Forzar ventana al frente al cargar
        this.Loaded += async (s, e) =>
        {
            this.WindowState = WindowState.Normal; // Asegurar estado normal
            _logService.Info($"MainWindow Loaded event - Ensuring visibility.");
            
            this.Activate();
            this.Focus();
            
            // Forzar al frente temporalmente para superar el foco de otras apps al inicio
            this.Topmost = true;
            await System.Threading.Tasks.Task.Delay(150);
            this.Topmost = false;
        };
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


}