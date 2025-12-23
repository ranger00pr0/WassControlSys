namespace WassControlSys.Models
{
    public class SystemUsage
    {
        public double CpuUsage { get; set; } // Percentage
        public double RamUsage { get; set; } // Percentage
        public double DiskUsage { get; set; } // Percentage
        public double[] CpuPerCore { get; set; } = Array.Empty<double>();
        public double NetBytesSentPerSec { get; set; }
        public double NetBytesReceivedPerSec { get; set; }
        public int ActiveTcpConnections { get; set; }
        public double DiskReadsPerSec { get; set; }
        public double DiskWritesPerSec { get; set; }
        public double DiskAvgQueueLength { get; set; }
        public double DiskReadLatencyMs { get; set; }
        public double DiskWriteLatencyMs { get; set; }
    }
}
