# TAREA-006: Implementar Configuración de la App (Fase 4 Inicio)

## Objetivo
Implementar la sección "Configuración" permitiendo al usuario personalizar el comportamiento y apariencia de la aplicación.

## Alcance
1.  **Backend (Lógica):**
    *   Actualizar `AppSettings` para incluir:
        *   `RunOnStartup` (bool)
        *   `AccentColor` (string - Hex Code)
    *   Implementar lógica para escribir en el Registro de Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) para el "Inicio con Windows".
    *   Implementar lógica para cambio dinámico de recursos de color en `App.xaml` (Runtime).

2.  **ViewModel:**
    *   Exponer propiedades `RunOnStartup` y `SelectedAccentColor` en `MainViewModel`.
    *   Manejar la persistencia de estos cambios.

3.  **Frontend (UI):**
    *   Diseñar la vista "Configuración" en `MainWindow.xaml`.
    *   Incluir un Toggle (CheckBox estilizado) para "Iniciar con Windows".
    *   Incluir selector de colores (Botones circulares con colores predefinidos: Azul, Verde, Rojo, Púrpura).

## Colores Predefinidos
*   Azul (Default): `#3B82F6`
*   Verde: `#10B981`
*   Rojo: `#EF4444`
*   Púrpura: `#8B5CF6`
*   Naranja: `#F97316`

## Pasos
1.  Modificar `Models/AppSettings` en `Core/SettingsAndLogging.cs`.
2.  Añadir lógica de registro de Windows en `MainViewModel` (o servicio auxiliar).
3.  Añadir lógica de cambio de tema en `App.xaml.cs` o helper.
4.  Implementar la UI en `MainWindow.xaml`.
