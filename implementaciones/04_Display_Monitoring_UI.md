# TAREA 04: Crear UI para Monitoreo Básico

**Fase:** 2 - Perfiles de Rendimiento y Monitoreo Activo
**Funcionalidad:** Monitoreo en Tiempo Real

## Objetivo
Añadir los elementos visuales a la `MainWindow` para mostrar los datos de uso del sistema (CPU, RAM, Disco) que proporcionará el `MonitoringService`.

## Instrucciones para el Frontend (WPF - XAML)

**Archivo:** `WassControlSys/MainWindow.xaml`

**Descripción del Cambio:**
1.  Añadir un `GroupBox` con el título "Monitor del Sistema".
2.  Dentro del `GroupBox`, usar un `Grid` para alinear etiquetas (`TextBlock`) y barras de progreso (`ProgressBar`).
3.  Cada barra de progreso estará enlazada a una propiedad en el `MainViewModel` que contendrá el valor de uso de CPU, RAM y Disco.

**Fragmento de Código de Referencia (a añadir dentro del `StackPanel` principal):**

```xml
<!-- AÑADIR ESTE BLOQUE DEBAJO DEL GROUPBOX "Selector de Modo" -->
<GroupBox Header="Monitor del Sistema" Margin="0,20,0,0">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Grid.Column="0" Text="CPU:" Margin="5" VerticalAlignment="Center"/>
        <ProgressBar Grid.Row="0" Grid.Column="1" Value="{Binding CpuUsage}" Maximum="100" Height="20" Margin="5"/>

        <TextBlock Grid.Row="1" Grid.Column="0" Text="RAM:" Margin="5" VerticalAlignment="Center"/>
        <ProgressBar Grid.Row="1" Grid.Column="1" Value="{Binding RamUsage}" Maximum="100" Height="20" Margin="5"/>

        <TextBlock Grid.Row="2" Grid.Column="0" Text="Disco:" Margin="5" VerticalAlignment="Center"/>
        <ProgressBar Grid.Row="2" Grid.Column="1" Value="{Binding DiskUsage}" Maximum="100" Height="20" Margin="5"/>
    </Grid>
</GroupBox>
```
*Nota: Las propiedades `CpuUsage`, `RamUsage` y `DiskUsage` se crearán y conectarán en la siguiente tarea de ViewModel.*
