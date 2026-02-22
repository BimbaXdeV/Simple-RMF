using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public static class EventController
    {
        private static readonly Dictionary<string, CancellationTokenSource> RunningTasks = [];

        public static void ToggleEvent(string eventName)
        {
            if (RunningTasks.TryGetValue(eventName, out CancellationTokenSource? runningCts))
            {
                runningCts.Cancel();
                RunningTasks.Remove(eventName);
            }
            else
            {
                BackgroundEvent? backgroundEvent = EventAssembler.GetEvent(eventName);
                if (backgroundEvent != null)
                {
                    CancellationTokenSource newCts = new();
                    _ = Task.Run(() => backgroundEvent.ExecuteEvAsync(newCts.Token), newCts.Token);
                    RunningTasks[eventName] = newCts;
                }
            }
        }

        public static void StopAllRunning()
        {
            foreach (var cts in RunningTasks.Values)
            {
                cts.Cancel();
            }
            RunningTasks.Clear();
        }
    }
}
