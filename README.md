# WassControlSys

<div align="center">

![Version](https://img.shields.io/badge/version-0.1.1-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![License](https://img.shields.io/badge/license-Proprietary-red.svg)

**Sistema de Control y Optimización para Windows**

Una aplicación completa para optimizar, limpiar y administrar tu sistema Windows con una interfaz moderna y elegante.

[Descargar](#-descarga) • [Características](#-características) • [Documentación](#-documentación) • [Contribuir](#-contribuir)

</div>

---

## 📸 Capturas de Pantalla

> _Próximamente: Capturas de pantalla de la aplicación_

---

## ✨ Características

### 🎨 Interfaz Moderna
- **Temas Dinámicos** - 5 colores de acento personalizables
- **Fuente Roboto** - Tipografía moderna y legible
- **Modo Oscuro** - Diseño elegante y cómodo para la vista
- **Controles Personalizados** - Barra de título integrada con el diseño

### 🧹 Limpieza del Sistema
- Archivos temporales del sistema
- Caché de navegadores
- Papelera de reciclaje
- Opciones personalizables

### ⚡ Optimización
- **Optimizar RAM** - Libera memoria de procesos inactivos
- **Limpiar DNS** - Mejora la velocidad de navegación
- **Análisis de Disco** - Verifica fragmentación (HDD)
- **Índice de Búsqueda** - Reconstruye para búsquedas más rápidas
- **Limpiar Prefetch** - Optimiza archivos de inicio
- **Reiniciar Red** - Soluciona problemas de conectividad

### 🔧 Diagnóstico
- **SFC** - System File Checker
- **DISM** - Reparación de imagen del sistema
- **CHKDSK** - Verificación de disco

### 🛡️ Seguridad
- Estado de Windows Defender
- Estado del Firewall
- Estado de UAC (Control de Cuentas de Usuario)

### 🔌 Administración de Servicios
- Ver todos los servicios de Windows
- Iniciar/Detener servicios
- Información detallada de cada servicio
- Búsqueda y filtrado

### 🚀 Programas de Inicio
- Administrar aplicaciones que inician con Windows
- Habilitar/Deshabilitar programas
- Mejorar tiempo de arranque

### 🗑️ Desinstalador de Bloatware
- Detectar aplicaciones preinstaladas
- Desinstalar software no deseado
- Liberar espacio en disco

### 🔒 Configuración de Privacidad
- Telemetría de Windows
- Servicios de ubicación
- Diagnósticos
- Personalización de privacidad

### ⚙️ Configuración
- Cambiar color de acento
- Ejecutar al iniciar Windows
- Persistencia de configuración

---

## 📥 Descarga

### Última Versión: v0.1.1

**[⬇️ Descargar WassControlSys_v0.1.1.zip](../../releases/latest)**

### Requisitos del Sistema
- **Sistema Operativo:** Windows 10 (64-bit) o superior
- **Framework:** .NET 8.0 Runtime (se descarga automáticamente)
- **RAM:** 2 GB mínimo (4 GB recomendado)
- **Espacio en Disco:** 50 MB

### Instalación

1. **Descargar** el archivo ZIP
2. **Extraer** en la ubicación deseada
3. **Ejecutar** `WassControlSys.exe`
4. Si no tienes .NET 8.0, Windows te pedirá instalarlo

> **Nota:** Para acceso completo a todas las funciones, ejecutar como administrador

---

## 🚀 Inicio Rápido

```bash
# 1. Clonar el repositorio
git clone https://github.com/TU_USUARIO/WassControlSys.git

# 2. Navegar al directorio
cd WassControlSys

# 3. Restaurar dependencias
dotnet restore

# 4. Compilar
dotnet build

# 5. Ejecutar
dotnet run
```

---

## 🏗️ Arquitectura

### Tecnologías Utilizadas
- **.NET 8.0** - Framework principal
- **WPF** - Windows Presentation Foundation
- **MVVM** - Patrón Model-View-ViewModel
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection

### Estructura del Proyecto

```
WassControlSys/
├── Core/                    # Servicios principales
│   ├── MonitoringService.cs
│   ├── SystemMaintenanceService.cs
│   ├── SecurityService.cs
│   └── ...
├── Models/                  # Modelos de datos
│   ├── PerformanceMode.cs
│   ├── SecurityStatus.cs
│   └── ...
├── ViewModels/              # ViewModels (MVVM)
│   └── MainViewModel.cs
├── Views/                   # Vistas de usuario
│   ├── DashboardView.xaml
│   ├── CleaningView.xaml
│   └── ...
├── App.xaml                 # Configuración de la aplicación
└── MainWindow.xaml          # Ventana principal
```

### Dependencias

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
<PackageReference Include="System.Management" Version="10.0.0" />
<PackageReference Include="System.ServiceProcess.ServiceController" Version="10.0.0" />
```

---

## 📖 Documentación

### Guías de Usuario
- [README de Usuario](publish/README.md) - Guía de inicio rápido
- [Notas de Versión](publish/RELEASE_NOTES_v0.1.1.md) - Cambios en v0.1.1

### Documentación Técnica
- [Guía de Compilación](BUILD_v0.1.1.md) - Cómo compilar el proyecto
- [Guía de Distribución](DISTRIBUTION_v0.1.1.md) - Cómo distribuir la aplicación

### Implementaciones
Ver la carpeta `implementaciones/` para documentación detallada de cada módulo:
- Sistema de Colores Dinámicos
- Tipografía Roboto
- Controles de Ventana
- Scroll en Servicios
- Y más...

---

## 🛠️ Desarrollo

### Compilar en Release

```bash
dotnet build -c Release
```

### Publicar Ejecutable

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

### Crear ZIP de Distribución

```powershell
Compress-Archive -Path ".\publish\WassControlSys.exe", ".\publish\README.md", ".\publish\RELEASE_NOTES_v0.1.1.md" -DestinationPath ".\WassControlSys_v0.1.1.zip" -Force
```

---

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Si quieres contribuir:

1. **Fork** el proyecto
2. **Crea** una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. **Push** a la rama (`git push origin feature/AmazingFeature`)
5. **Abre** un Pull Request

### Reportar Problemas

Si encuentras un bug o tienes una sugerencia:
1. Verifica que no exista un issue similar
2. Crea un nuevo issue con:
   - Descripción clara del problema
   - Pasos para reproducir
   - Versión de Windows
   - Capturas de pantalla (si aplica)

---

## 🗺️ Roadmap

### v0.2.0 (Próxima)
- [ ] Modo claro/oscuro
- [ ] Más colores de acento personalizables
- [ ] Exportar reportes del sistema
- [ ] Programador de tareas
- [ ] Actualizaciones automáticas
- [ ] Icono de aplicación

### v0.3.0 (Futuro)
- [ ] Monitor de red en tiempo real
- [ ] Gestor de variables de entorno
- [ ] Editor de archivo hosts
- [ ] Limpiador de registro
- [ ] Gestor de puntos de restauración
- [ ] Visor de logs del sistema

---

## 📝 Changelog

### v0.1.1 (8 de Diciembre, 2025)

#### ✨ Nuevas Características
- Sistema de colores dinámicos (5 colores)
- Tipografía Roboto en toda la aplicación
- Controles de ventana personalizados
- Módulo de optimización expandido (6 herramientas)
- Opciones de limpieza personalizables
- Vista de servicios mejorada con scroll
- Resaltado de navegación activa

#### 🔧 Mejoras
- Mejor legibilidad con fuentes más grandes
- Word wrap en nombres de servicios
- Feedback mejorado en comandos de diagnóstico
- Arquitectura con Dependency Injection

Ver [RELEASE_NOTES_v0.1.1.md](publish/RELEASE_NOTES_v0.1.1.md) para detalles completos.

---

## ⚠️ Advertencias

- **Permisos de Administrador:** Algunas funciones requieren ejecutar como administrador
- **Servicios Críticos:** No se pueden detener servicios esenciales del sistema
- **Backup:** Se recomienda crear un punto de restauración antes de hacer cambios importantes
- **Antivirus:** Algunos antivirus pueden dar falsos positivos (ejecutable sin firma digital)

---

## 📄 Licencia

Copyright © 2025 WassControl. Todos los derechos reservados.

Este software es propietario. Ver el archivo LICENSE para más detalles.

---

## 🙏 Agradecimientos

- **Microsoft** - Por .NET y WPF
- **Google Fonts** - Por la fuente Roboto
- **Comunidad de GitHub** - Por las herramientas y recursos

---

## 📞 Contacto

- **Issues:** [GitHub Issues](../../issues)
- **Discussions:** [GitHub Discussions](../../discussions)

---

<div align="center">

**Hecho con ❤️ para la comunidad de Windows**

[⬆ Volver arriba](#wasscontrolsys)

</div>
