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

        public async Task<ApplyProfileResult> ApplyProfileAsync(PerformanceMode mode)
        {
            // Por ahora, aplicamos el plan de energía como acción segura y visible.
            return await Task.Run(() => TrySetPowerPlan(mode));
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
