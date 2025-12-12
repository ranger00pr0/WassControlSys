# WassControlSys
Sistema de Control y Optimización para Windows

<div align="center">

[![Version](https://img.shields.io/badge/version-0.1.1-blue.svg)](https://github.com/ranger00pr0/WassControlSys) [![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)](https://github.com/ranger00pr0/WassControlSys) [![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://github.com/ranger00pr0/WassControlSys) [![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

**Sistema de Control y Optimización para Windows**

Una aplicación para optimizar, limpiar y administrar tu sistema Windows con una interfaz moderna y funcional.

[Descargar](#descarga) • [Características](#características) • [Documentación](#documentación) • [Contribuir](#contribuir)

</div>

---

## 📸 Capturas de Pantalla

_Próximamente: capturas de la aplicación._  
(Coloca imágenes en `docs/images` y pon rutas relativas en el README.)

---

## ✨ Características

### 🎨 Interfaz
- Temas dinámicos y modo oscuro
- Tipografía moderna (ej. Roboto)
- Controles integrados en la ventana (barra de título personalizada)

### 🧹 Limpieza del sistema
- Limpieza de archivos temporales y caché de navegadores
- Vaciar papelera y limpiar Prefetch
- Opciones personalizables por usuario

### ⚡ Optimización
- Optimizar RAM liberando procesos inactivos
- Limpiar DNS y reiniciar red
- Análisis de disco y reconstrucción de índice de búsqueda

### 🔧 Diagnóstico
- Ejecutar SFC, DISM y CHKDSK desde la interfaz
- Reportes y logs de acciones

### 🛡️ Seguridad
- Estado de Windows Defender, Firewall y UAC
- Opciones básicas de privacidad y telemetría

### 🔌 Administración de servicios
- Listado, inicio/detención y detalles de servicios
- Búsqueda y filtrado

### 🚀 Programas de inicio
- Administrar aplicaciones que arrancan con Windows
- Habilitar/Deshabilitar entradas de inicio

### 🗑️ Desinstalador de bloatware
- Detectar y eliminar aplicaciones preinstaladas no deseadas

---

## 📥 Descarga

### Última versión: v0.1.1

[⬇️ Descargar WassControlSys_v0.1.1.zip](https://github.com/ranger00pr0/WassControlSys/releases/latest/download/WassControlSys_v0.1.1.zip)

### Requisitos mínimos
- Windows 10 (64-bit) o superior  
- .NET 8.0 Runtime (o la versión que el proyecto requiera)  
- 2 GB RAM mínimo (4 GB recomendado)  
- ~50 MB de espacio libre

> Ejecutar como administrador para acceder a todas las funciones.

---

## 🚀 Inicio rápido (desarrollo)

```bash
# Clonar el repositorio
git clone https://github.com/ranger00pr0/WassControlSys.git
cd WassControlSys

# Restaurar y compilar
dotnet restore
dotnet build

# Ejecutar (ajusta la ruta al .csproj si hace falta)
dotnet run --project ./Ruta/AlProyecto.csproj
```

---

## 🏗️ Arquitectura y tecnologías

- .NET 8.0 (o la versión indicada)  
- WPF (interfaz) — patrón MVVM  
- Dependency Injection con Microsoft.Extensions.DependencyInjection

Estructura típica:
```
WassControlSys/
├── Core/
├── Models/
├── ViewModels/
├── Views/
├── App.xaml
└── MainWindow.xaml
```

Dependencias de ejemplo (NuGet):
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
<PackageReference Include="System.Management" Version="10.0.0" />
```

---

## 📖 Documentación
- Guía de usuario: README.md (esta página)  
- Notas de versión: ver sección Changelog  
- Documentación técnica en `implementaciones/` y `docs/` (si aplica)

---

## 🛠️ Desarrollo y distribución

Compilar en modo Release:
```bash
dotnet build -c Release
```

Publicar ejecutable (ejemplo win-x64):
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

Crear ZIP de distribución (PowerShell):
```powershell
Compress-Archive -Path ".\publish\*" -DestinationPath ".\WassControlSys_v0.1.1.zip" -Force
```

---

## 🤝 Contribuir

1. Haz fork del repositorio.  
2. Crea una rama: `git checkout -b feature/nombre`.  
3. Haz commits claros y push.  
4. Abre un Pull Request describiendo los cambios.  

Antes de reportar un bug, busca si ya existe un issue similar y añade pasos para reproducir, versión de Windows y capturas si aplican.

---

## 🗺️ Roadmap (resumen)

Próximas ideas:
- Mejoras en temas y personalización
- Exportar reportes del sistema
- Programador de tareas y actualizaciones automáticas
- Monitor de red en tiempo real
- Limpiador del registro (opcional, con advertencias)

---

## 📝 Changelog (resumen v0.1.1)
- Sistema de colores dinámicos y tipografía Roboto  
- Módulo de optimización ampliado  
- Mejoras en la vista de servicios y legibilidad

(Actualizar con fechas y detalles reales según avance.)

---

## ⚠️ Advertencias
- Algunas acciones requieren permisos de administrador.  
- No detener servicios críticos del sistema.  
- Recomendado crear un punto de restauración antes de cambios importantes.  
- Ejecutables sin firma pueden dar falsos positivos en antivirus.

---

## 📄 Licencia
Copyright © 2025 WassControl.  
Este software es propietario. Ver el archivo LICENSE para más detalles.

---

## 🙏 Agradecimientos
- Microsoft (.NET & WPF)  
- Google Fonts (Roboto)  
- Comunidad de GitHub

---

## 📞 Contacto
- Issues: https://github.com/ranger00pr0/WassControlSys/issues  
- Discussions: https://github.com/ranger00pr0/WassControlSys/discussions

---

Hecho con ❤️ para la comunidad de Windows.
