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
        public bool IsEvRunning { get; }

        public Task ExecuteAsync(ISession session, CancellationToken token);
    }
}
