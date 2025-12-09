# TAREA-007: Implementar Módulo de Seguridad (Fase 4 Completada)

## Objetivo
Implementar la sección "Seguridad" para proporcionar al usuario un resumen del estado de seguridad básico de su sistema.

## Alcance
1.  **Backend (Servicio de Seguridad):**
    *   Crear `Core/SecurityService.cs`.
    *   Implementar métodos para verificar:
        *   **Antivirus:** Detectar producto instalado y estado (Activo/Inactivo) mediante WMI (`ROOT\SecurityCenter2`).
        *   **Firewall:** Verificar estado del Firewall de Windows mediante WMI o API de Windows.
        *   **UAC (User Account Control):** Verificar nivel de notificación en el registro.

2.  **Modelo de Datos:**
    *   Crear clase `SecurityStatus` con propiedades:
        *   `AntivirusName` (string)
        *   `AntivirusStatus` (enum/bool: Protected/NotProtected)
        *   `FirewallEnabled` (bool)
        *   `UacEnabled` (bool)

3.  **ViewModel:**
    *   Integrar `SecurityService` en `MainViewModel`.
    *   Exponer propiedad `SecurityStatus`.
    *   Cargar estado asíncronamente al iniciar o al navegar a la sección.

4.  **Frontend (UI):**
    *   Diseñar vista en `MainWindow.xaml` para la sección `Seguridad`.
    *   Mostrar tarjetas de estado con iconos (Verde = OK, Rojo = Alerta).
    *   (Opcional) Botón para "Abrir Seguridad de Windows".

## Detalles Técnicos
*   **WMI Namespace:** `ROOT\SecurityCenter2` es necesario para info de Antivirus (requiere `System.Management`).
*   **Registry UAC:** `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` -> `EnableLUA`.

## Pasos
1.  Crear `Core/SecurityService.cs`.
2.  Actualizar `MainViewModel.cs`.
3.  Diseñar UI en `MainWindow.xaml`.
