# Arquitectura del Sistema

WassControlSys está construido utilizando **WPF (Windows Presentation Foundation)** sobre **.NET 8.0**, siguiendo estrictamente el patrón de diseño **MVVM (Model-View-ViewModel)** y principios de **Clean Architecture**.

## 🏗️ Estructura de Alto Nivel

El proyecto se divide en las siguientes capas lógicas:

### 1. Views (Vistas - UI)

- Responsable únicamente de la presentación y la interacción con el usuario.
- **Tecnología**: XAML.
- **Ubicación**: Carpeta `Views/` y `MainWindow.xaml`.
- **Características**:
  - Uso de `UserControls` para navegación modular.
  - Estilos y recursos centralizados en `App.xaml`.
  - `WindowChrome` personalizado para una apariencia moderna (sin barra de título estándar).

### 2. ViewModels (Lógica de Presentación)

- Actúa como intermediario entre la Vista y el Modelo/Servicios.
- Gestiona el estado de la aplicación y expone comandos (`ICommand`).
- **Ubicación**: Carpeta `ViewModels/`.
- **Componente Principal**: `MainViewModel.cs`.
  - Gestiona la navegación (`CurrentSection`).
  - Centraliza la inyección de todos los servicios.
  - Implementa `INotifyPropertyChanged` para el databinding.

### 3. Models (Datos)

- Representa las entidades de datos del dominio.
- Objetos POCO (Plain Old CLR Objects) simples.
- **Ubicación**: Carpeta `Models/`.
- **Ejemplos**: `SystemInfo`, `BloatwareApp`, `ServiceInfo`.

### 4. Core / Services (Lógica de Negocio e Infraestructura)

- Contiene la lógica pesada y el acceso a APIs del sistema.
- **Ubicación**: Carpeta `Core/`.
- **Implementación**:
  - Interfaces (`ISystemMaintenanceService`, `IBloatwareService`) para desacople y testabilidad.
  - Implementaciones concretas que usan:
    - `System.Management` (WMI) para hardware y servicios.
    - `Microsoft.Win32.Registry` para bloatware y configuraciones.
    - `System.Diagnostics.Process` para ejecutar comandos de sistema (`cmd`, `powershell`, `defrag`).

## 💉 Inyección de Dependencias (DI)

Utilizamos `Microsoft.Extensions.DependencyInjection` para gestionar el ciclo de vida de los servicios.

- La configuración se realiza en `App.xaml.cs`.
- Todos los servicios se registran como `Singleton` o `Transient` según necesidad.
- `MainViewModel` recibe sus dependencias vía constructor.

## 🔄 Flujo de Datos

1. **Usuario** interactúa con la **Vista** (ej. clic en "Limpiar").
2. **Vista** ejecuta un `ICommand` en el **ViewModel**.
3. **ViewModel** llama a un método asíncrono en un **Servicio** (`Core`).
4. **Servicio** ejecuta la operación (ej. borrar archivo, consulta WMI) en un hilo de fondo (`Task.Run`).
5. **ViewModel** recibe el resultado y actualiza sus propiedades observables.
6. **Vista** refleja los cambios automáticamente gracias al Databinding.

## 🛡️ Consideraciones de Seguridad

- **Ejecución Elevada**: Métodos críticos (`LaunchElevated`) solicitan permisos de administrador mediante el verbo `runas`.
- **Validación**: Los servicios validan rutas y entradas antes de ejecutar comandos destructivos (ej. limpieza de archivos).
- **Manejo de Errores**: Bloques `try-catch` robustos en todos los puntos de integración con el sistema operativo para evitar crashes.
