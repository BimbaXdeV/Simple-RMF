using RMF.Core.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
{
    public interface IEvent
    {
        public bool IsEvRunning { get; }

        public Task ExecuteAsync(ClientSession session, CancellationToken token);
    }
}
