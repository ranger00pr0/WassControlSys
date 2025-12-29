using System;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IMonitoringService : IDisposable
    {
        SystemUsage GetSystemUsage();
        TimeSpan GetIdleTime();
    }
}
