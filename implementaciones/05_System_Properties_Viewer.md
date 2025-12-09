# TAREA-005: Visor de Propiedades del Sistema

## Objetivo
Implementar una nueva sección "Sistema" que muestre información detallada del hardware y software del equipo.

## Alcance
1.  **Backend (Servicio):** Crear `SystemInfoService` para recopilar:
    *   Nombre del Equipo
    *   Versión del Sistema Operativo
    *   Procesador (Nombre/Modelo)
    *   Memoria RAM Total
    *   Tarjeta Gráfica (GPU) - *Opcional si requiere dependencias complejas, pero intentaremos WMI*
    *   Espacio en Disco (Sistema)

2.  **ViewModel:**
    *   Integrar `SystemInfoService` en `MainViewModel`.
    *   Crear propiedades enlazables para mostrar la data.

3.  **Frontend (UI):**
    *   Añadir entrada "Sistema" en el menú lateral.
    *   Crear la vista correspondiente con un diseño atractivo (tarjetas o lista de detalles).

## Dependencias
*   Es probable que necesitemos instalar `System.Management` para obtener detalles de CPU y GPU.

## Pasos
1.  Añadir el paquete `System.Management`.
2.  Crear `Core/SystemInfoService.cs`.
3.  Actualizar `Models/AppSection.cs` y `MainWindow.xaml` (Menú).
4.  Consumir el servicio en `MainViewModel` y exponer propiedades.
5.  Diseñar la vista en `MainWindow.xaml`.
