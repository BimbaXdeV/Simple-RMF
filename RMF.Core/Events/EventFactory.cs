using RMF.Core.Events.Client;
using RMF.Core.Events.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventFactory : IEventFactory
    {
        private readonly Dictionary<string, (Func<IEvent> Factory, EventOrigin Origin)> _factories;

        public EventFactory()
        {
            _factories = new Dictionary<string, (Func<IEvent>, EventOrigin)>()
            {
                // [!] Client-side
                { nameof(StreamingEvent), (() => new StreamingEvent(), EventOrigin.Client) },

                // [!] Server-side
                { nameof(HeartbeatEvent), (() => new HeartbeatEvent(), EventOrigin.Server) }
            };
        }

        public IEvent? CreateEvent(string eventName)
        {
            return _factories.TryGetValue(eventName, out var tuple)
                ? tuple.Factory()
                : null;
        }
    }
}
