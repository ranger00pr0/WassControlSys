# Release Notes - WassControlSys v1.1.2 🛠️

Esta versión es una actualización de **estabilidad crítica** que soluciona problemas reportados por los usuarios en la versión anterior.

## 🐛 Correcciones y Mejoras

### ⚙️ Estabilidad del Sistema

- **Corrección de Congelamientos:** Las herramientas de mantenimiento (SFC, DISM, CHKDSK, Reset Red) ahora se ejecutan de forma asíncrona, evitando que la interfaz se congele durante operaciones largas.
- **Prevención de Crashes:** Se solucionó un error crítico en la pestaña "Métricas Avanzadas" que cerraba la aplicación inesperadamente al actualizar la información de los núcleos de CPU.
- **Inicio de Aplicación:** Subsanado el error `XamlParseException` que impedía el arranque en algunos sistemas.

### 🛡️ Seguridad y Restauración

- **Estado de Protección:** Mejorada la detección de Antivirus y Firewall (soporte para múltiples antivirus y detección por máscara de bits).
- **Puntos de Restauración:** El Dashboard ahora muestra el nombre y fecha del último Punto de Restauración del sistema.

### 📊 Interfaz y Usabilidad

- **Pestaña "Discos":** Renombrada (antes "Estado de Discos") para mayor claridad.
- **Configuración:** Mejorada la visibilidad del selector de idioma y añadidos iconos para el Modo Oscuro.
- **Logs de Diagnóstico:** Nuevo botón en configuración para abrir la carpeta de registros y facilitar el soporte técnico.
- **Gestor de Procesos:** Lista de procesos optimizada con auto-refresco cada 30 segundos.

---

**Desarrollado con ❤️ por WilmerWass**
