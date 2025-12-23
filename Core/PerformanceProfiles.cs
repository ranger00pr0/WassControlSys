using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WassControlSys.Models
{
    public enum PerformanceMode
    {
        General = 0,
        Gamer = 1,
        Dev = 2,
        Oficina = 3
    }

    public class ApplyProfileResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}

namespace WassControlSys.Core
{
    using WassControlSys.Models;

    public class PerformanceProfileService : IPerformanceProfileService
    {
        // GUIDs estándar conocidos de planes de energía de Windows
        private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";      // Equilibrado
        private const string HighPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";       // Alto rendimiento
        private const string PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";     // Ahorro de energía (referencia)
        private readonly IProcessManagerService? _processManager;
        private readonly IServiceOptimizerService? _serviceOptimizer;
        private readonly ILogService? _log;

        public PerformanceProfileService(IProcessManagerService? processManager = null, IServiceOptimizerService? serviceOptimizer = null, ILogService? log = null)
        {
            _processManager = processManager;
            _serviceOptimizer = serviceOptimizer;
            _log = log;
        }

        public async Task<ApplyProfileResult> ApplyProfileAsync(PerformanceMode mode)
        {
            var power = await Task.Run(() => TrySetPowerPlan(mode));
            try
            {
                switch (mode)
                {
                    case PerformanceMode.Gamer:
                        if (_processManager != null)
                            await _processManager.ReduceBackgroundProcessesAsync(ProcessPriorityClass.BelowNormal);
                        if (_serviceOptimizer != null)
                            await ApplyServiceAdjustmentsAsync(mode);
                        break;
                    case PerformanceMode.Oficina:
                        // Mantener equilibrio, sin cambios agresivos
                        if (_serviceOptimizer != null)
                            await ApplyServiceAdjustmentsAsync(mode);
                        break;
                    case PerformanceMode.Dev:
                        // Sin acciones adicionales por ahora
                        if (_serviceOptimizer != null)
                            await ApplyServiceAdjustmentsAsync(mode);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _log?.Warn($"Acciones adicionales de perfil fallaron: {ex.Message}");
            }
            return power;
        }

        private async Task ApplyServiceAdjustmentsAsync(PerformanceMode mode)
        {
            if (_serviceOptimizer == null) return;
            switch (mode)
            {
                case PerformanceMode.Gamer:
                    await _serviceOptimizer.SetServiceStartTypeAsync("SysMain", ServiceStartType.Manual);
                    await _serviceOptimizer.StopServiceAsync("SysMain");
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Manual);
                    await _serviceOptimizer.StopServiceAsync("WSearch");
                    break;
                case PerformanceMode.Oficina:
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Automatic);
                    await _serviceOptimizer.StartServiceAsync("WSearch");
                    await _serviceOptimizer.SetServiceStartTypeAsync("SysMain", ServiceStartType.Automatic);
                    break;
                case PerformanceMode.Dev:
                    await _serviceOptimizer.SetServiceStartTypeAsync("WSearch", ServiceStartType.Manual);
                    break;
                default:
                    break;
            }
        }

        private ApplyProfileResult TrySetPowerPlan(PerformanceMode mode)
        {
            string guid = GetPowerPlanGuidForMode(mode);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c powercfg -setactive {guid}",
                    UseShellExecute = true,
                    Verb = "runas", // eleva UAC
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process? p = Process.Start(psi);
                if (p == null)
                {
                    return new ApplyProfileResult { Success = false, Message = "No se pudo iniciar powercfg." };
                }
                return new ApplyProfileResult { Success = true, Message = $"Plan de energía aplicado: {mode}." };
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return new ApplyProfileResult { Success = false, Message = "Operación cancelada por el usuario (UAC)." };
            }
            catch (Exception ex)
            {
                return new ApplyProfileResult { Success = false, Message = $"Error al aplicar el perfil: {ex.Message}" };
            }
        }

        private static string GetPowerPlanGuidForMode(PerformanceMode mode)
        {
            return mode switch
            {
                PerformanceMode.Gamer => HighPerfGuid,
                PerformanceMode.Dev => BalancedGuid, // se puede ajustar a Alto rendimiento si se desea
                PerformanceMode.Oficina => BalancedGuid,
                _ => BalancedGuid,
            };
        }
    }
}
