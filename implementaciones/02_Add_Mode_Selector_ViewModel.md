# TAREA 02: Añadir Lógica de ViewModel para Selector de Modo

**Fase:** 2 - Perfiles de Rendimiento y Monitoreo Activo
**Funcionalidad:** Selector de Modos

## Objetivo
Añadir la lógica en el `MainViewModel` para gestionar el estado del selector de modo. Esto implica crear un `Enum` para los modos y una propiedad en el ViewModel para almacenar la selección actual.

## Instrucciones para el Backend (C#)

### 1. Crear el Enum `PerformanceMode`

**Archivo a Crear:** `WassControlSys/Models/PerformanceMode.cs`

**Contenido:**
```csharp
namespace WassControlSys.Models
{
    public enum PerformanceMode
    {
        General,
        Gamer,
        Dev,
        Oficina
    }
}
```

### 2. Actualizar el `MainViewModel`

**Archivo:** `WassControlSys/ViewModels/MainViewModel.cs`

**Descripción del Cambio:**
1.  Añadir una nueva propiedad `CurrentMode` del tipo `PerformanceMode`.
2.  La propiedad debe implementar `INotifyPropertyChanged` para que la UI se actualice cuando cambie.
3.  Inicializar `CurrentMode` a `PerformanceMode.General` en el constructor.
4.  Añadir un `using WassControlSys.Models;` al inicio del archivo.

**Fragmento de Código a Añadir (dentro de `MainViewModel.cs`):**

```csharp
// Añadir al inicio del archivo
using WassControlSys.Models;

// Añadir esta propiedad dentro de la clase MainViewModel
private PerformanceMode _currentMode;
public PerformanceMode CurrentMode
{
    get => _currentMode;
    set
    {
        if (_currentMode != value)
        {
            _currentMode = value;
            OnPropertyChanged();
            // Aquí se podría añadir lógica para cuando el modo cambia
            MessageBox.Show($"Modo cambiado a: {value}", "Selector de Modo");
        }
    }
}

// Añadir esta línea en el constructor de MainViewModel
CurrentMode = PerformanceMode.General;
```
