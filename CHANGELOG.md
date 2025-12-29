# Changelog

Todas las mejoras notables de este proyecto serán documentadas en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [1.1.7] - 2026-01-02

### Añadido

- **Editor de Perfiles de Rendimiento**: Nueva herramienta para personalizar qué acciones se ejecutan en cada modo (Gamer, Dev, Oficina).
- **Modo Personalizado (Custom)**: Capacidad de crear y guardar una configuración de optimización desde cero.
- **Lógica de Reversibilidad**: Sistema que restaura el estado original de servicios y procesos al desactivar un modo.
- **Descripciones Técnicas**: Añadidas advertencias e información detalla en cada opción del editor para mayor seguridad del usuario.
- **Persistencia de Perfiles**: Opción para recordar el perfil activo tras reiniciar el sistema.

### Cambiado

- **IPerformanceModeService**: Refactorización del servicio para soportar configuraciones dinámicas y perfiles de usuario.

## [1.1.6] - 2025-12-25

### Añadido

- **Panel de Comprobación de Mantenimiento (Health Check)**: Se ha rediseñado completamente la vista principal para centralizar el estado del sistema en un solo lugar.
- **PC Boost**: Nueva función para optimizar el rendimiento del sistema con un solo clic desde el panel principal.
- **Resumen de Estado Centralizado**: El nuevo panel muestra resúmenes directos de Seguridad, Aplicaciones de Inicio, y estado de Limpieza.
- **Comprobación de Windows Update**: El servicio de seguridad ahora verifica el estado de las actualizaciones de Windows.
- **Análisis de Limpieza Mejorado**: La función de limpieza ahora puede analizar la carpeta de Descargas y buscar archivos grandes.

### Cambiado

- **Rediseño de la Interfaz Principal**: La `DashboardView` ha sido modernizada para ofrecer una experiencia de usuario más intuitiva y centralizada.



## [1.1.5] - 2025-12-22

### Añadido

- **Ejecución en Segundo Plano (System Tray)**: La aplicación ahora se minimiza al área de notificación del sistema en lugar de cerrarse, permitiendo que las tareas de monitoreo continúen en segundo plano. Incluye un menú contextual para abrir o salir.

### Corregido

- **Winget**: Robustecido el parseo de la salida de Winget para evitar errores "Index and length", y mejor manejo de códigos de salida.
- **Servicios de Windows**: Manejo específico de excepciones de "Acceso Denegado" para informar correctamente al usuario sobre la necesidad de privilegios administrativos.
- **Puntos de Restauración**: Advertencia controlada en lugar de error crítico al consultar puntos de restauración sin permisos.
- **Resolución de Conflictos**: Corrección de ambigüedades de tipos entre WPF y Windows Forms (`Color`, `Application`, `Binding`) al integrar el System Tray.

## [1.1.4] - 2025-12-21

### Añadido

- **Progreso de Tareas en UI**:
  - La exportación de drivers ahora muestra una barra de progreso y estado en la misma vista, eliminando la ventana de terminal.
  - La actualización de aplicaciones con Winget ahora muestra una barra de progreso y estado para cada aplicación individualmente.
- **Analizador de Espacio Dinámico**: La vista de "Discos" ahora detecta y muestra un analizador de espacio para cada unidad de disco duro presente en el sistema, en lugar de solo C: y D:.
- **Liberador de Espacio en Disco**: Se ha añadido un botón y un selector en la vista de "Discos" para lanzar la utilidad de limpieza de disco de Windows (`cleanmgr.exe`) para una unidad específica.

### Cambiado

- **Proceso de Actualización de Apps**: Las actualizaciones de aplicaciones que no provienen de la Microsoft Store (`msstore`) ahora piden confirmación al usuario antes de proceder.
- **Detección de Antivirus y Firewall**: Se ha mejorado la lógica de detección en el `SecurityService` para interpretar de forma más fiable el estado de los productos de seguridad reportado por WMI.
- **Mensajes de Error**: Mejorado el mensaje de error al fallar una actualización de Winget para guiar al usuario.

### Corregido

- **Contraste de UI**: Solucionado un problema de contraste en el `ComboBox` de selección de idioma en la vista de configuración, mejorando la legibilidad.

### Eliminado

- **(Opcional) Barra de Progreso de Desinstalación**: Se canceló la implementación de la barra de progreso para la desinstalación de bloatware debido a la alta complejidad y falta de un método estándar y seguro para obtener el progreso de los diferentes tipos de desinstaladores.

## [1.1.1] - 2025-12-18

### Añadido

- **Sistema de Temas Dinámico**: Implementación completa de Modos Claro y Oscuro con persistencia de configuración.
- **Acceso Directo al Menú**: Sección de "Inicio Principal" rediseñada para mayor claridad.
- **Mejoras en Aplicaciones (Winget)**:
  - Añadido botón de cancelación de búsqueda.
  - Indicador de progreso local y estado de búsqueda en tiempo real.
- **Diseño de Botones Estándar**: Nueva paleta de estilos para botones de acción (`Primary`, `Secondary`, `Danger`).
- **Tooltips Informativos**: Añadidos en toda la sección de Mantenimiento para explicar cada función de reparación (SFC, DISM, CHKDSK).

### Cambiado

- **Refactorización de Interfaz**: Eliminación de colores fijos para total compatibilidad con temas dinámicos.
- **Optimización de Rendimiento de UI**: Habilitada la virtualización en todas las listas largas para mayor fluidez.
- **Flujo de Inicio**: El modo de rendimiento por defecto ahora se establece correctamente en "General" antes de cargar la configuración del usuario.

### Corregido

- Corregida visibilidad de texto en diversas secciones (Hardware, Rendimiento, Aplicaciones) al usar temas oscuros.
- Eliminados parpadeos visuales al cambiar entre secciones del menú.
- Corregido error en el ViewModel que impedía la correcta actualización de la lista de apps.

## [0.2.0] - 2025-12-16

### Añadido

- **Módulo de Información del Sistema**: Ahora muestra versión de BIOS, información detallada de red (IP, adaptador) y tiempo de actividad (uptime) del sistema.
- **Optimizador de Sistema**:
  - Limpieza de caché DNS (`flushdns`).
  - Análisis de fragmentación de disco (`defrag /A`).
  - Limpieza de carpeta Prefetch con manejo de permisos.
  - Reinicio de configuración de red (`winsock reset`, `int ip reset`).
  - Reconstrucción de íncide de búsqueda de Windows.
- **Desinstalador de Bloatware**:
  - Detección mejorada escaneando claves de registro de usuario (HKCU) y sistema (HKLM).
  - Nueva heurística para filtrar aplicaciones críticas vs. aplicaciones seguras de eliminar.
- **Interfaz Gráfica**:
  - Nuevos indicadores de carga y mensajes de estado en la barra inferior.
  - Mejoras en la navegación y consistencia visual (Roboto, iconos).
- **Core**:
  - Implementación de métodos asíncronos reales para todas las tareas de mantenimiento.
  - `SystemInfoService` expandido para mayor detalle de hardware.
  - `BloatwareService` refactorizado para mayor seguridad.

### Cambiado

- **MainViewModel**: Refactorización masiva para eliminar código duplicado y centralizar la lógica de ejecución en servicios.
- **App.xaml**: Corrección crítica en la definición de `ResourceDictionary` que causaba cierres inesperados (`XamlParseException`).
- **Licencia**: Confirmación de licencia MIT para el proyecto.

## [0.1.1] - 2025-12-15

### Añadido

- Estructura base del proyecto WPF con MVVM.
- Inyección de dependencias (`Microsoft.Extensions.DependencyInjection`).
- Vistas básicas: Dashboard, Limpieza, Optimización, Servicios.
- Sistema de temas y localización (Español/Inglés).
- Control de ventana personalizado (Chrome).

### Corregido

- Error inicial de compilación por falta de etiquetas de cierre en XAML.
