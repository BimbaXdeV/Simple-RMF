using RMF.Core.Bases;
using RMF.Core.Interfaces;
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
        private readonly ConcurrentDictionary<string, EventContainer> RunningTasks = [];

        public void StartEvent(ClientSession session, string eventName, Dictionary<string, object>? eventSettings = null)
        {
            if (!this.RunningTasks.ContainsKey(eventName))
            {
                IEvent? backgroundEvent = EventAssembler.GetEvent(eventName);
                if (backgroundEvent != null)
                {
                    if (eventSettings != null)
                    {
                        EventAssembler.ApplyEventSettings(backgroundEvent, eventSettings);
                    }
                    CancellationTokenSource newCts = new();
                    EventContainer container = new(backgroundEvent, newCts);

                    if (this.RunningTasks.TryAdd(eventName, container))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await backgroundEvent.ExecuteAsync(session, newCts.Token);
                            }
                            finally
                            {
                                this.RunningTasks.TryRemove(eventName, out _);
                                newCts.Dispose();
                            }

                            //_ = Task.Run(() => backgroundEvent.ExecuteAsync(session, newCts.Token), newCts.Token);
                            //this.RunningTasks[eventName] = new EventContainer(backgroundEvent, newCts);
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
            if (this.RunningTasks.TryGetValue(eventName, out EventContainer? container))
            {
                container.Cts?.Cancel();
                this.RunningTasks.Remove(eventName, out _);
            }
        }

        public bool IsRunning(string eventName)
        {
            return this.RunningTasks.ContainsKey(eventName);
        }

        public void StopAllRunning()
        {
            foreach (EventContainer container in this.RunningTasks.Values)
            {
                container.Cts?.Cancel();
            }
        }
    }
}
