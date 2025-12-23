# Contribuir a WassControlSys

¡Gracias por tu interés en contribuir a WassControlSys! Este documento proporciona pautas para contribuir al proyecto.

## 📋 Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [Cómo Contribuir](#cómo-contribuir)
- [Reportar Bugs](#reportar-bugs)
- [Sugerir Mejoras](#sugerir-mejoras)
- [Pull Requests](#pull-requests)
- [Guía de Estilo](#guía-de-estilo)
- [Configuración del Entorno](#configuración-del-entorno)

---

## 📜 Código de Conducta

Este proyecto se adhiere a un código de conducta. Al participar, se espera que mantengas este código. Por favor reporta comportamientos inaceptables abriendo un issue.

### Nuestros Estándares

- Usar lenguaje acogedor e inclusivo
- Respetar diferentes puntos de vista y experiencias
- Aceptar críticas constructivas con gracia
- Enfocarse en lo que es mejor para la comunidad
- Mostrar empatía hacia otros miembros de la comunidad

---

## 🤝 Cómo Contribuir

### 1. Fork del Repositorio

```bash
# Haz fork del repositorio en GitHub
# Luego clona tu fork
git clone https://github.com/WilmerWass/WassControlSys.git
cd WassControlSys
```

### 2. Crear una Rama

```bash
# Crea una rama para tu feature o fix
git checkout -b feature/mi-nueva-caracteristica
# o
git checkout -b fix/correccion-de-bug
```

### 3. Hacer Cambios

- Escribe código limpio y bien documentado
- Sigue las convenciones de código del proyecto
- Agrega comentarios donde sea necesario
- Actualiza la documentación si es necesario

### 4. Commit de Cambios

```bash
# Agrega tus cambios
git add .

# Commit con un mensaje descriptivo
git commit -m "feat: agregar nueva funcionalidad X"
```

#### Convención de Mensajes de Commit

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` Nueva característica
- `fix:` Corrección de bug
- `docs:` Cambios en documentación
- `style:` Cambios de formato (no afectan el código)
- `refactor:` Refactorización de código
- `test:` Agregar o modificar tests
- `chore:` Cambios en el proceso de build o herramientas

### 5. Push y Pull Request

```bash
# Push a tu fork
git push origin feature/mi-nueva-caracteristica

# Abre un Pull Request en GitHub
```

---

## 🐛 Reportar Bugs

Si encuentras un bug, por favor crea un issue con:

### Información Requerida

- **Título claro y descriptivo**
- **Descripción detallada del problema**
- **Pasos para reproducir:**
  1. Ir a '...'
  2. Click en '...'
  3. Ver error
- **Comportamiento esperado**
- **Comportamiento actual**
- **Capturas de pantalla** (si aplica)
- **Información del sistema:**
  - Versión de Windows
  - Versión de la aplicación
  - Versión de .NET

### Ejemplo de Issue

```markdown
**Descripción**
La aplicación se cierra al intentar limpiar archivos temporales.

**Pasos para Reproducir**
1. Abrir WassControlSys
2. Ir a sección "Limpieza"
3. Click en "Iniciar Limpieza"
4. La aplicación se cierra

**Comportamiento Esperado**
La limpieza debería completarse sin errores.

**Sistema**
- Windows 11 Pro 64-bit
- WassControlSys v1.1.4
- .NET 8.0

**Logs**
[Adjuntar app.log si está disponible]
```

---

## 💡 Sugerir Mejoras

Para sugerir una nueva característica:

1. **Verifica** que no exista un issue similar
2. **Crea un issue** con la etiqueta `enhancement`
3. **Describe** la funcionalidad deseada
4. **Explica** por qué sería útil
5. **Proporciona** ejemplos de uso

---

## 🔀 Pull Requests

### Checklist antes de Enviar

- [ ] El código compila sin errores
- [ ] El código sigue las convenciones del proyecto
- [ ] Los cambios están documentados
- [ ] Se han actualizado los archivos README si es necesario
- [ ] Los commits tienen mensajes descriptivos
- [ ] Se ha probado en Windows 10 y/o 11

### Proceso de Revisión

1. Un mantenedor revisará tu PR
2. Pueden solicitar cambios o mejoras
3. Una vez aprobado, se hará merge
4. Tu contribución será incluida en la próxima release

---

## 🎨 Guía de Estilo

### C# Code Style

```csharp
// Usar PascalCase para clases y métodos
public class MiClase
{
    // Usar camelCase para variables privadas con _
    private readonly IService _service;
    
    // Usar PascalCase para propiedades
    public string MiPropiedad { get; set; }
    
    // Métodos con nombres descriptivos
    public async Task EjecutarOperacionAsync()
    {
        // Código aquí
    }
}
```

### XAML Style

```xml
<!-- Usar indentación de 4 espacios -->
<Border Background="{StaticResource SurfaceBrush}" 
        CornerRadius="12" 
        Padding="20">
    <StackPanel>
        <!-- Contenido -->
    </StackPanel>
</Border>
```

### Nomenclatura

- **Archivos:** PascalCase (ej: `MainViewModel.cs`)
- **Carpetas:** PascalCase (ej: `ViewModels/`)
- **Recursos:** PascalCase (ej: `PrimaryBrush`)
- **Comandos:** PascalCase + "Command" (ej: `CleanTempFilesCommand`)

---

## 🛠️ Configuración del Entorno

### Requisitos

- Visual Studio 2022 o superior
- .NET 8.0 SDK
- Git

### Configuración

```bash
# 1. Clonar el repositorio
git clone https://github.com/WilmerWass/WassControlSys.git
cd WassControlSys

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Compilar
dotnet build

# 4. Ejecutar
dotnet run
```

### Estructura de Ramas

- `main` - Rama principal (estable)
- `develop` - Rama de desarrollo
- `feature/*` - Ramas de nuevas características
- `fix/*` - Ramas de correcciones

---

## 📚 Recursos Útiles

### Documentación
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [MVVM Pattern](https://docs.microsoft.com/dotnet/architecture/maui/mvvm)

### Herramientas
- [Visual Studio](https://visualstudio.microsoft.com/)
- [Git](https://git-scm.com/)
- [GitHub Desktop](https://desktop.github.com/)

---

## ❓ Preguntas

Si tienes preguntas sobre cómo contribuir:

1. Revisa la documentación existente
2. Busca en issues cerrados
3. Abre un nuevo issue con la etiqueta `question`
4. Únete a las discusiones en GitHub Discussions

---

## 🎉 Reconocimientos

Todos los contribuidores serán reconocidos en:
- El archivo README.md
- Las notas de release
- La sección de contribuidores de GitHub

---

## 📝 Licencia

Al contribuir a WassControlSys, aceptas que tus contribuciones se licenciarán bajo la misma licencia MIT del proyecto.

---

**¡Gracias por contribuir a WassControlSys!** 🚀
