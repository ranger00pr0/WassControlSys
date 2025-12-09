# Controles de Ventana - WassControlSys

## 🪟 Implementación de Barra de Título Personalizada

### Objetivo
Agregar controles de ventana personalizados (minimizar, maximizar/restaurar, cerrar) con una barra de título moderna y coherente con el diseño de la aplicación.

---

## ✅ Características Implementadas

### 1. **Barra de Título Personalizada**

#### Diseño
- **Altura:** 32px
- **Fondo:** #1A1A1A (oscuro)
- **Borde inferior:** #333 (sutil separación)
- **Título:** "WassControlSys" con color de acento dinámico

#### Características
```xml
<Border Grid.Row="0" Background="#1A1A1A" BorderBrush="#333" BorderThickness="0,0,0,1">
    <Grid>
        <!-- Title -->
        <TextBlock Text="WassControlSys" 
                   Foreground="{DynamicResource PrimaryBrush}"/>
        
        <!-- Window Controls -->
        <StackPanel WindowChrome.IsHitTestVisibleInChrome="True">
            <!-- Buttons here -->
        </StackPanel>
    </Grid>
</Border>
```

### 2. **Botones de Control**

#### Botón Minimizar (🗕)
- **Función:** Minimiza la ventana a la barra de tareas
- **Hover:** Fondo #2A2A2A
- **Tamaño:** 46x32px

#### Botón Maximizar/Restaurar (🗖/🗗)
- **Función:** Alterna entre maximizado y normal
- **Icono Dinámico:**
  - 🗖 cuando está en modo normal
  - 🗗 cuando está maximizada
- **Hover:** Fondo #2A2A2A
- **Tamaño:** 46x32px

#### Botón Cerrar (✕)
- **Función:** Cierra la aplicación
- **Hover:** Fondo #E81123 (rojo Windows)
- **Tamaño:** 46x32px

### 3. **Funcionalidad del Code-Behind**

```csharp
private void MinimizeButton_Click(object sender, RoutedEventArgs e)
{
    this.WindowState = WindowState.Minimized;
}

private void MaximizeButton_Click(object sender, RoutedEventArgs e)
{
    if (this.WindowState == WindowState.Maximized)
        this.WindowState = WindowState.Normal;
    else
        this.WindowState = WindowState.Maximized;
    
    UpdateMaximizeButtonIcon();
}

private void CloseButton_Click(object sender, RoutedEventArgs e)
{
    this.Close();
}

private void UpdateMaximizeButtonIcon()
{
    if (MaximizeButton != null)
    {
        MaximizeButton.Content = this.WindowState == WindowState.Maximized ? "🗗" : "🗖";
    }
}
```

### 4. **WindowChrome Configuration**

```xml
<WindowChrome.WindowChrome>
    <WindowChrome CaptionHeight="32" 
                  ResizeBorderThickness="4" 
                  GlassFrameThickness="0" 
                  CornerRadius="0"/>
</WindowChrome.WindowChrome>
```

**Propiedades:**
- **CaptionHeight:** 32px (altura de la barra de título)
- **ResizeBorderThickness:** 4px (área para redimensionar)
- **GlassFrameThickness:** 0 (sin efecto glass)
- **CornerRadius:** 0 (esquinas rectas)

---

## 🎨 Diseño Visual

### Estructura de la Ventana

```
┌─────────────────────────────────────────────────┐
│ WassControlSys              [🗕] [🗖] [✕]      │ ← Barra de título (32px)
├─────────────────────────────────────────────────┤
│                                                 │
│                                                 │
│              Contenido Principal                │
│                                                 │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Efectos Hover

| Botón | Color Normal | Color Hover |
|-------|--------------|-------------|
| Minimizar | Transparente | #2A2A2A |
| Maximizar | Transparente | #2A2A2A |
| Cerrar | Transparente | **#E81123** (Rojo) |

### Título
- **Fuente:** Roboto, 13px, SemiBold
- **Color:** Dinámico (usa el color de acento seleccionado)
- **Posición:** Izquierda, centrado verticalmente

---

## 🔧 Funcionalidades

### ✅ Minimizar
- Click en 🗕
- Ventana se minimiza a la barra de tareas
- Puede restaurarse desde la barra de tareas

### ✅ Maximizar/Restaurar
- Click en 🗖 (normal) → Maximiza
- Click en 🗗 (maximizada) → Restaura
- El icono cambia automáticamente
- También funciona con doble click en la barra de título (WindowChrome)

### ✅ Cerrar
- Click en ✕
- Cierra la aplicación completamente
- Ejecuta el método `Close()` que puede ser interceptado si se necesita confirmación

### ✅ Redimensionar
- Arrastrar desde los bordes (4px de grosor)
- Funciona en todos los lados y esquinas
- Cursor cambia automáticamente

### ✅ Mover
- Arrastrar desde la barra de título
- Funciona en toda el área de la barra excepto en los botones

---

## 💡 Ventajas del Diseño

### 1. **Consistencia Visual**
- Barra de título integrada con el diseño oscuro
- Usa el color de acento de la aplicación
- No usa la barra de título estándar de Windows

### 2. **Experiencia Moderna**
- Botones minimalistas con iconos Unicode
- Efectos hover sutiles
- Botón cerrar con color rojo distintivo

### 3. **Funcionalidad Completa**
- Todos los controles estándar funcionan
- Redimensionar desde cualquier borde
- Mover arrastrando la barra

### 4. **Responsive**
- El icono de maximizar cambia según el estado
- Los botones responden al hover
- La ventana recuerda su tamaño y posición

---

## 📝 Notas Técnicas

### WindowChrome.IsHitTestVisibleInChrome
```xml
<StackPanel WindowChrome.IsHitTestVisibleInChrome="True">
```
Esta propiedad es **crucial** para que los botones sean clickeables en el área del WindowChrome.

### Iconos Unicode
Los iconos usados son caracteres Unicode:
- 🗕 (U+1F5D5) - Minimize
- 🗖 (U+1F5D6) - Maximize
- 🗗 (U+1F5D7) - Restore
- ✕ (U+2715) - Close

**Ventajas:**
- No requieren imágenes
- Escalan perfectamente
- Fáciles de cambiar

**Alternativa:** Usar Path con geometrías SVG para más control

### StateChanged Event
```csharp
this.StateChanged += (s, e) => UpdateMaximizeButtonIcon();
```
Actualiza el icono cuando la ventana cambia de estado (por ejemplo, al hacer doble click en la barra de título).

---

## 🚀 Mejoras Futuras Posibles

### 1. **Animaciones**
- Transición suave al maximizar/restaurar
- Fade in/out en hover

### 2. **Botones Adicionales**
- Botón de configuración rápida
- Botón de ayuda
- Indicador de notificaciones

### 3. **Doble Click en Título**
- Ya funciona por WindowChrome
- Podría personalizarse más

### 4. **Menú Contextual**
- Click derecho en la barra de título
- Opciones: Minimizar, Maximizar, Cerrar, Siempre encima

### 5. **Iconos Personalizados**
- Usar Path con geometrías SVG
- Mayor control sobre el diseño
- Animaciones más complejas

---

## ✅ Verificación

Para probar los controles:

1. **Minimizar:**
   - Click en 🗕
   - Verificar que la ventana se minimiza
   - Restaurar desde la barra de tareas

2. **Maximizar:**
   - Click en 🗖
   - Verificar que la ventana se maximiza
   - Verificar que el icono cambia a 🗗

3. **Restaurar:**
   - Click en 🗗 (cuando está maximizada)
   - Verificar que vuelve al tamaño normal
   - Verificar que el icono cambia a 🗖

4. **Cerrar:**
   - Click en ✕
   - Verificar que la aplicación se cierra

5. **Redimensionar:**
   - Arrastrar desde cualquier borde
   - Verificar que el cursor cambia
   - Verificar que redimensiona correctamente

6. **Mover:**
   - Arrastrar desde la barra de título
   - Verificar que la ventana se mueve

---

## 🎉 Resultado

La aplicación ahora tiene:
- ✅ **Barra de título personalizada** integrada con el diseño
- ✅ **Controles de ventana funcionales** (minimizar, maximizar, cerrar)
- ✅ **Diseño moderno** con efectos hover
- ✅ **Icono dinámico** para maximizar/restaurar
- ✅ **Redimensionamiento** desde todos los bordes
- ✅ **Experiencia de usuario completa**
