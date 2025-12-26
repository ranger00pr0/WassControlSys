using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class ProcessManagerService : IProcessManagerService
    {
        private readonly ILogService _log;

        public ProcessManagerService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<ProcessInfoDto>> GetProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<ProcessInfoDto>();
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        var info = new ProcessInfoDto
                        {
                            Pid = p.Id,
                            Name = p.ProcessName,
                            Priority = p.PriorityClass,
                            WorkingSetMb = p.WorkingSet64 / (1024.0 * 1024.0),
                            StartTime = SafeStartTime(p),
                            IsForeground = !string.IsNullOrEmpty(p.MainWindowTitle) || p.MainWindowHandle != IntPtr.Zero
                        };
                        list.Add(info);
                    }
                    catch { }
                }
                return list.OrderByDescending(x => x.WorkingSetMb);
            });
        }

        public async Task<bool> SetPriorityAsync(int pid, ProcessPriorityClass priority)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    p.PriorityClass = priority;
                    _log.Info($"Priority changed for {p.ProcessName}({pid}) to {priority}");
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error changing priority for pid {pid}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> KillProcessAsync(int pid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    p.Kill(true);
                    _log.Warn($"Process killed {p.ProcessName}({pid})");
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error killing pid {pid}", ex);
                    return false;
                }
            });
        }

        public async Task<ProcessImpactStats> ComputeImpactAsync()
        {
            return await Task.Run(() =>
            {
                double totalMb = 0;
                int count = 0;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        totalMb += p.WorkingSet64 / (1024.0 * 1024.0);
                        count++;
                    }
                    catch { }
                }
                return new ProcessImpactStats
                {
                    ProcessCount = count,
                    TotalWorkingSetMb = totalMb
                };
            });
        }

        public async Task<int> ReduceBackgroundProcessesAsync(ProcessPriorityClass targetPriority)
        {
            return await Task.Run(() =>
            {
                int changed = 0;
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (IsBackgroundCandidate(p))
                        {
                            p.PriorityClass = targetPriority;
                            changed++;
                        }
                    }
                    catch { }
                }
                _log.Info($"Background reduction applied to {changed} processes");
                return changed;
            });
        }

        private static DateTime? SafeStartTime(Process p)
        {
            try { return p.StartTime; } catch { return null; }
        }

        private static bool IsBackgroundCandidate(Process p)
        {
            try
            {
                var name = p.ProcessName.ToLowerInvariant();
                if (name.Contains("system") || name.Contains("idle")) return false;
                if (p.SessionId != 1 && p.MainWindowHandle == IntPtr.Zero) return true;
                return false;
            }
            catch { return false; }
        }
    }
}

