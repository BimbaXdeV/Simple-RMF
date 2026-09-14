using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF.Core.Security;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class ServerStatusCommand : InlineCommand
    {
        private readonly IServerMetricsMonitor _metrics;
        private readonly IServerSessionManager _sessionManager;
        private readonly IFirewall _firewall;
        private readonly IThemeManager _themeManager;

        private const byte CpuCoresMediumThreshold = 4;
        private const byte CpuCoresNormalThreshold = 8;
        private const byte CpuMediumLoadThreshold = 30;
        private const byte CpuHighLoadThreshold = 60;
        private const byte TotalRamGbMediumThreshold = 2;
        private const byte TotalRamGbNormalThreshold = 4;
        private const byte RamGbUsageMediumThreshold = 20;
        private const byte RamGbUsageHighThreshold = 40;

        public override string Category => "Performance";
        public override string Name => "status";
        public override string Description => "Displays server metrics: server status, network sessions, and overall machine load";
        public override string[]? Parameters => null;

        public ServerStatusCommand(

            IServerMetricsMonitor metrics,
            IServerSessionManager sessionManager,
            IFirewall firewall,
            IThemeManager themeManager,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _metrics = metrics;
            _sessionManager = sessionManager;
            _firewall = firewall;
            _themeManager = themeManager;
        }

        private static string GetStateColorKey(double currentValue, double secondThreshold, double thirdThreshold, bool reverse = false)
        {
            if (!reverse)
            {
                return currentValue switch
                {
                    _ when currentValue < secondThreshold => "MachineLoadNormal",
                    _ when currentValue < thirdThreshold => "MachineLoadMedium",
                    _ => "MachineLoadHigh"
                };
            }
            else
            {
                return currentValue switch
                {
                    _ when currentValue < secondThreshold => "MachineLoadHigh",
                    _ when currentValue < thirdThreshold => "MachineLoadMedium",
                    _ => "MachineLoadNormal"
                };
            }
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            int cpuCores = _metrics.CoresCount;
            double cpuLoad = _metrics.GetCpuLoadPercentage();
            RmfRamUsage ramUsage = _metrics.GetRamUsage();

            ThemeColor statusColor = _themeManager.GetColor("ServerStatus");
            ThemeColor hintColor = _themeManager.GetColor("ServerStatusHint");
            ThemeColor cpuLoadColor = _themeManager.GetColor(GetStateColorKey(cpuLoad, CpuMediumLoadThreshold, CpuHighLoadThreshold));
            ThemeColor cpuCoresColor = _themeManager.GetColor(GetStateColorKey(cpuCores, CpuCoresMediumThreshold, CpuCoresNormalThreshold, reverse: true));
            ThemeColor totalRamColor = _themeManager.GetColor(GetStateColorKey(ramUsage.TotalMemoryGb, TotalRamGbMediumThreshold, TotalRamGbNormalThreshold, reverse: true));
            ThemeColor usedRamColor = _themeManager.GetColor(GetStateColorKey(ramUsage.UsedMemoryGb, RamGbUsageMediumThreshold, RamGbUsageHighThreshold));

            CmLogger.LogInformation("Server status ({StatusColorStart}Online{StatusColorEnd})", statusColor, ThemeColor.AnsiReset);
            CmLogger.LogInformation("- Server uptime        : {Uptime}", (DateTime.Now - _metrics.ProcessStartTime).ToString(RmfConstants.TimeSpanFormatHms));
            CmLogger.LogInformation("- Active connections   : {ActiveConnections}  {HintColorStart}(more in: \"/conlst\"){HintColorEnd}",
                _sessionManager.TotalConnections, hintColor, ThemeColor.AnsiReset
            );
            CmLogger.LogInformation("- Connection IP filters: {BannedIPs}  {HintColorStart}(more in: \"/banlst\"){HintColorEnd}",
                _firewall.GetBannedIPsCount(), hintColor, ThemeColor.AnsiReset
            );
            CmLogger.LogInformation("- CPU total load       : {CpuColorStart}{CpuLoad:f2}%{CpuColorEnd}  ({CoresColorStart}{CoresCount}{CoresColorEnd} cores)",
                cpuLoadColor, cpuLoad, ThemeColor.AnsiReset, cpuCoresColor, cpuCores, ThemeColor.AnsiReset
            );
            CmLogger.LogInformation("- Server RAM           : {TRamColorStart}{TotalRam:f3} GB{TRamColorEnd},  Used: {URamColorStart}{UsedRam:f3} GB{URamColorEnd}",
                totalRamColor, ramUsage.TotalMemoryGb, ThemeColor.AnsiReset, usedRamColor, ramUsage.UsedMemoryGb, ThemeColor.AnsiReset
            );
            return Task.CompletedTask;
        }
    }
}
