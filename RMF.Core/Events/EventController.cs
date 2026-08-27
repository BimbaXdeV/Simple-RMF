using RMF.Core.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventController
    {
        private readonly IEventFactory _eventFactory;

        private readonly ConcurrentDictionary<string, EventContainer> _runningTasks;

        public EventController(IEventFactory eventFactory)
        {
            this._eventFactory = eventFactory;

            this._runningTasks = new ConcurrentDictionary<string, EventContainer>();
        }

        public void StartEvent(
            ISession session,
            string eventName,
            Dictionary<string, object>? eventSettings = null
        )
        {
            if (!this._runningTasks.ContainsKey(eventName))
            {
                IEvent? backgroundEvent = this._eventFactory.CreateEvent(eventName);
                if (backgroundEvent != null)
                {
                    if (eventSettings != null)
                    {
                        this._eventFactory.ApplyEventSettings(backgroundEvent, eventSettings);
                    }
                    CancellationTokenSource newCts = new();
                    EventContainer container = new(backgroundEvent, newCts);

                    if (this._runningTasks.TryAdd(eventName, container))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await backgroundEvent.ExecuteAsync(session, newCts.Token);
                            }
                            finally
                            {
                                this._runningTasks.TryRemove(eventName, out _);
                                newCts.Dispose();
                            }
                        }, newCts.Token);
                    }
                    else
                    {
                        newCts.Dispose();
                    }
                }
            }
        }

        public void StopEvent(string eventName)
        {
            if (this._runningTasks.TryGetValue(eventName, out EventContainer? container))
            {
                container.Cts?.Cancel();
                this._runningTasks.Remove(eventName, out _);
            }
        }

        public bool IsRunning(string eventName)
        {
            return this._runningTasks.ContainsKey(eventName);
        }

        public void StopAllRunning()
        {
            foreach (EventContainer container in this._runningTasks.Values)
            {
                container.Cts?.Cancel();
            }
        }
    }
}
