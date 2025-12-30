# Notas de la Versión v1.1.5

## 🚀 Nuevas Características

- **Ejecución en Segundo Plano (System Tray)**: La aplicación ahora se minimiza al área de notificación del sistema en lugar de cerrarse, permitiendo que las tareas de monitoreo continúen en segundo plano. Incluye un menú contextual para abrir o salir.

## 🐛 Correcciones de Errores

- **Winget**: Se ha robustecido el parseo de la salida de Winget para evitar errores al leer la lista de actualizaciones ("Index and length must refer to a location within the string").
- **Servicios de Windows**: Mejorado el manejo de errores de permisos. Ahora se notifica claramente si se requiere ejecutar como administrador para modificar servicios, evitando cierres inesperados.
- **Puntos de Restauración**: Corregido un estancamiento/error al consultar puntos de restauración sin permisos de administrador.

## 🔧 Mejoras Técnicas

- Resolución de conflictos de tipos entre WPF y Windows Forms.
- Actualización de `NotifyIcon` para extraer correctamente el icono del ejecutable.
