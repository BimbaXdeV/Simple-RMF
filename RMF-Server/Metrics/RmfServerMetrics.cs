using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Metrics
{
    internal class RmfServerMetrics : IServerMetricsMonitor
    {
        private readonly Process _currentProcess;
        private TimeSpan _lastCpuTime;
        private DateTime _lastSnapshotTime;

        public DateTime ProcessStartTime => this._currentProcess.StartTime;
        public int CoresCount => Environment.ProcessorCount;

        public RmfServerMetrics()
        {
            this._currentProcess = Process.GetCurrentProcess();
            this._lastCpuTime = TimeSpan.Zero;
            this._lastSnapshotTime = DateTime.MinValue;
        }

        public RmfRamUsage GetRamUsage()
        {
            double total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0;
            double used = this._currentProcess.WorkingSet64 / 1024.0 / 1024.0 / 1024.0;
            return new RmfRamUsage(total, used);
        }

        public double GetCpuLoadPercentage()
        {
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan currentCpuTime = this._currentProcess.TotalProcessorTime;

            if (this._lastSnapshotTime == DateTime.MinValue)
            {
                this._lastSnapshotTime = currentTime;
                this._lastCpuTime = currentCpuTime;
                return 0.0;
            }

            double timeWindowMs = (currentTime - this._lastSnapshotTime).TotalMilliseconds;
            double totalCpuUsedMs = (currentCpuTime - this._lastCpuTime).TotalMilliseconds;

            this._lastSnapshotTime = currentTime;
            this._lastCpuTime = currentCpuTime;

            if (timeWindowMs <= 0)
            {
                return 0.0;
            }

            return (totalCpuUsedMs / (timeWindowMs * this.CoresCount)) * 100.0;
        }
    }
}
