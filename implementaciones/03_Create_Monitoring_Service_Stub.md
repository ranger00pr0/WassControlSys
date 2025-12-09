# TAREA 03: Crear Stub del Servicio de Monitoreo

**Fase:** 2 - Perfiles de Rendimiento y Monitoreo Activo
**Funcionalidad:** Monitoreo en Tiempo Real

## Objetivo
Crear la estructura básica para el `MonitoringService`. Este servicio se encargará en el futuro de obtener los datos reales de uso de CPU, RAM y Disco. En este paso, solo devolverá valores fijos o aleatorios.

## Instrucciones para el Backend (C#)

### 1. Crear el Modelo de Datos `SystemUsage`

**Archivo a Crear:** `WassControlSys/Models/SystemUsage.cs`

**Contenido:**
```csharp
namespace WassControlSys.Models
{
    public class SystemUsage
    {
        public double CpuUsage { get; set; } // Porcentaje
        public double RamUsage { get; set; } // Porcentaje
        public double DiskUsage { get; set; } // Porcentaje
    }
}
```

### 2. Crear el `MonitoringService`

**Archivo a Crear:** `WassControlSys/Core/MonitoringService.cs`

**Descripción del Cambio:**
Crear una nueva clase `MonitoringService` que tenga un método público `GetSystemUsage()`. Por ahora, este método devolverá datos de ejemplo.

**Contenido:**
```csharp
using WassControlSys.Models;
using System;

namespace WassControlSys.Core
{
    public class MonitoringService
    {
        private readonly Random _random = new Random();

        public SystemUsage GetSystemUsage()
        {
            // En el futuro, aquí se usará PerformanceCounter u otras APIs
            // para obtener los datos reales.
            return new SystemUsage
            {
                CpuUsage = _random.Next(5, 25),   // Simula un uso de CPU entre 5% y 25%
                RamUsage = _random.Next(30, 60),  // Simula un uso de RAM entre 30% y 60%
                DiskUsage = _random.Next(20, 70)  // Simula un uso de Disco entre 20% y 70%
            };
        }
    }
}
```
