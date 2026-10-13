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
            _eventFactory = eventFactory;

            _runningTasks = new ConcurrentDictionary<string, EventContainer>();
        }

        public EventResult StartEvent(
            ISession session,
            string eventName,
            Dictionary<string, object>? eventSettings = null
        )
        {
            if (_runningTasks.ContainsKey(eventName))
            {
                return EventResult.Failure($"Event \"{eventName}\" is already running");
            }

            IEvent? backgroundEvent = _eventFactory.CreateEvent(eventName);
            if (backgroundEvent == null)
            {
                return EventResult.Failure($"Event \"{eventName}\" is not registered in the EventFactory");
            }

            if (eventSettings != null)
            {
                backgroundEvent.ApplySettings(eventSettings);
            }

            if (!backgroundEvent.ValidateSettings(out string? exceptionMessage))
            {
                return EventResult.Failure($"Settings are invalid: {exceptionMessage}");
            }

            CancellationTokenSource newCts = new();
            EventContainer container = new(backgroundEvent, newCts);

            if (_runningTasks.TryAdd(eventName, container))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await backgroundEvent.ExecuteAsync(session, newCts.Token);
                    }
                    // [!] Catching exceptions is not recommended, as both parts of the system use
                    // different output engine. However, if the event stops working for some reason,
                    // the first step is to uncomment the exception-catching code below:

                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine(ex);
                    //}
                    finally
                    {
                        StopEvent(container);
                    }
                }, newCts.Token);
            }

            return EventResult.Success();
        }

        private void StopEvent(EventContainer eventContainer)
        {
            eventContainer.Dispose();
            _runningTasks.Remove(nameof(eventContainer.Event), out _);
        }

        public void StopEvent(string eventName)
        {
            if (_runningTasks.TryGetValue(eventName, out EventContainer? container))
            {
                container.Dispose();
                _runningTasks.Remove(eventName, out _);
            }
        }

        public bool IsRunning(string eventName)
        {
            return _runningTasks.ContainsKey(eventName);
        }

        public void StopAllRunning()
        {
            foreach (EventContainer container in _runningTasks.Values)
            {
                StopEvent(container);
            }
        }
    }
}
