using System;
using System.Diagnostics;

namespace WassControlSys.Models
{
    public class ProcessInfoDto
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProcessPriorityClass Priority { get; set; }
        public double WorkingSetMb { get; set; }
        public DateTime? StartTime { get; set; }
        public bool IsForeground { get; set; }
    }

    public class ProcessImpactStats
    {
        public int ProcessCount { get; set; }
        public double TotalWorkingSetMb { get; set; }
    }
}

