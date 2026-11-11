using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public interface IEvent
    {
        bool IsEvRunning { get; }

        bool ValidateSettings(out string? exceptionMessage);
        void ApplySettings(IReadOnlyDictionary<string, object> settings);
        Task ExecuteAsync(ISession session, CancellationToken token);
    }
}
