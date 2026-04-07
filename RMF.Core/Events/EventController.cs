using RMF.Core.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventController
    {
        private readonly Dictionary<string, CancellationTokenSource> RunningTasks = [];

        public void ToggleEvent(ClientSession session, string eventName, Dictionary<string, object>? eventSettings = null)
        {
            if (this.RunningTasks.TryGetValue(eventName, out CancellationTokenSource? runningCts))
            {
                runningCts.Cancel();
                this.RunningTasks.Remove(eventName);
            }
            else
            {
                BackgroundEvent? backgroundEvent = EventAssembler.GetEvent(eventName);
                if (backgroundEvent != null)
                {
                    if (eventSettings != null)
                    {
                        EventAssembler.ApplyEventSettings(backgroundEvent, eventSettings);
                    }

                    CancellationTokenSource newCts = new();
                    _ = Task.Run(() => backgroundEvent.ExecuteEvAsync(session, newCts.Token), newCts.Token);
                    this.RunningTasks[eventName] = newCts;
                }
            }
        }

        public bool IsRunning(string eventName)
        {
            return this.RunningTasks.ContainsKey(eventName);
        }

        public void StopAllRunning()
        {
            foreach (var cts in this.RunningTasks.Values)
            {
                cts.Cancel();
            }
            // this.RunningTasks.Clear();
        }
    }
}
