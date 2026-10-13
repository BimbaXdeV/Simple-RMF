using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventContainer : IDisposable
    {
        public DateTime StartTime;
        public IEvent? Event;
        public CancellationTokenSource? Cts;

        public EventContainer()
        {
            StartTime = DateTime.Now;
        }

        public EventContainer(IEvent backgroundEvent, CancellationTokenSource cts)
        {
            StartTime = DateTime.Now;
            Event = backgroundEvent;
            Cts = cts;
        }

        public void Dispose()
        {
            Cts?.Cancel();
            Cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
