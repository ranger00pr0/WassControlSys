# TAREA 01: Crear UI para Selector de Modo

**Fase:** 2 - Perfiles de Rendimiento y Monitoreo Activo
**Funcionalidad:** Selector de Modos

## Objetivo
Añadir los controles visuales (un grupo de `RadioButton`) a la `MainWindow` que permitirán al usuario ver y seleccionar los diferentes perfiles de rendimiento (General, Gamer, Dev, Oficina).

## Instrucciones para el Frontend (WPF - XAML)

**Archivo:** `WassControlSys/MainWindow.xaml`

**Descripción del Cambio:**
1.  Localizar el `StackPanel` principal.
2.  Añadir un `GroupBox` con el título "Selector de Modo".
3.  Dentro del `GroupBox`, añadir un `StackPanel` horizontal.
4.  Dentro de este `StackPanel` horizontal, añadir cuatro `RadioButton` para los modos: "General", "Gamer", "Dev" y "Oficina".
5.  Cada `RadioButton` debe estar enlazado a la misma propiedad en el ViewModel (que crearemos en la Tarea 02), usando un `Converter` para asociar cada botón a un valor de `Enum` específico. Por ahora, solo definiremos la estructura básica.

**Fragmento de Código de Referencia (a añadir dentro del `StackPanel` principal):**

```xml
<!-- AÑADIR ESTE BLOQUE DEBAJO DEL BOTÓN EXISTENTE -->
<GroupBox Header="Selector de Modo" Margin="0,20,0,0">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <RadioButton Content="General" Margin="5" IsChecked="True"/>
        <RadioButton Content="Gamer" Margin="5"/>
        <RadioButton Content="Dev" Margin="5"/>
        <RadioButton Content="Oficina" Margin="5"/>
    </StackPanel>
</GroupBox>
```
