using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public interface IEventFactory
    {
        int RegisteredCount { get; }

        IEvent? CreateEvent(string eventName);
    }
}
