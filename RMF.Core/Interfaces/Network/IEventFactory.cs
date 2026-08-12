using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IEventFactory
    {
        IEvent? CreateEvent(string eventName);
        void ApplyEventSettings(string eventName, Dictionary<string, object> settings);
        void ApplyEventSettings(IEvent backgroundEvent, Dictionary<string, object> settings);
    }
}
