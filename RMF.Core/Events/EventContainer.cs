using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventContainer
    {
        public DateTime StartTime;
        public IEvent? Event;
        public CancellationTokenSource? Cts;

        public EventContainer()
        {
            this.StartTime = DateTime.Now;
        }

        public EventContainer(IEvent backgroundEvent, CancellationTokenSource cts)
        {
            this.StartTime = DateTime.Now;
            this.Event = backgroundEvent;
            this.Cts = cts;
        }
    }
}
