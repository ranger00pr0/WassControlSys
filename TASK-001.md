# TAREA-001: Implementar Limpiador de Espacio (Fase 1: Núcleo de Mantenimiento)

## Objetivo
Añadir una funcionalidad básica para limpiar archivos temporales, con un botón en la interfaz y una lógica inicial en el ViewModel.

## Instrucciones para el Frontend (WPF - XAML)

**Archivo:** `WassControlSys/MainWindow.xaml`

**Descripción del Cambio:**
Dentro del `<Grid>` principal, añadir un `StackPanel` para organizar los elementos verticalmente. El `TextBlock` existente se moverá dentro de este `StackPanel`. Debajo del `TextBlock`, se añadirá un `Button` con el texto "Limpiar Archivos Temporales". Este botón debe estar centrado horizontalmente y su propiedad `Command` debe enlazarse a un `ICommand` llamado `CleanTempFilesCommand` en el `MainViewModel`.

**Fragmento de Código a Añadir/Modificar:**

```xml
<Grid>
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="{Binding WelcomeMessage}" HorizontalAlignment="Center" VerticalAlignment="Center" FontSize="24" Margin="0,0,0,20"/>
        <Button Content="Limpiar Archivos Temporales" Command="{Binding CleanTempFilesCommand}" Width="200" Height="40"/>
    </StackPanel>
</Grid>
```

## Instrucciones para el Backend (C# - ViewModel)

**Archivo:** `WassControlSys/ViewModels/MainViewModel.cs`

**Descripción del Cambio:**
1.  Añadir una nueva propiedad `ICommand` llamada `CleanTempFilesCommand`.
2.  Inicializar `CleanTempFilesCommand` en el constructor de `MainViewModel` utilizando una clase `RelayCommand` (que deberemos crear en la carpeta `Core`).
3.  Crear un método `ExecuteCleanTempFiles` que será invocado por el comando. Por ahora, este método mostrará un `MessageBox` para confirmar que el comando se ejecutó.

**Fragmento de Código a Añadir/Modificar:**

```csharp
using System.Windows; // Necesario para MessageBox

// Dentro de la clase MainViewModel
public ICommand CleanTempFilesCommand { get; private set; }

// Dentro del constructor de MainViewModel
public MainViewModel()
{
    WelcomeMessage = "¡Bienvenido a WassControlSys! (Fase 1: Núcleo de Mantenimiento)";
    CleanTempFilesCommand = new RelayCommand(ExecuteCleanTempFiles);
}

private void ExecuteCleanTempFiles(object parameter)
{
    MessageBox.Show("Comando Limpiar Archivos Temporales ejecutado.", "Limpiador de Espacio", MessageBoxButton.OK, MessageBoxImage.Information);
    // Aquí se implementará la lógica real de limpieza en fases posteriores
}
```

**Archivo:** `WassControlSys/Core/RelayCommand.cs` (Nuevo archivo)

**Descripción del Cambio:**
Crear una implementación básica de `ICommand` para facilitar el enlace de comandos en WPF.

**Contenido del Archivo `RelayCommand.cs`:**

```csharp
using System;
using System.Windows.Input;

namespace WassControlSys.Core
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
```
