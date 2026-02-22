using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public class EventAssembler
    {
        private static readonly string EntryNamespaceFormat = "{}.Event.{}";
        public static readonly Dictionary<string, BackgroundEvent> Events = [];

        public static (int, int) RegisterFound(string category)
        {
            string? projectName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (string.IsNullOrEmpty(projectName))
            {
                return (0, 0);
            }

            category = char.ToUpper(category[0]) + category.Substring(1).ToLower();  // You can enter the name in any case
            string targetNamespace = string.Format(EntryNamespaceFormat, projectName, category);
            
            Type baseEventType = typeof(BackgroundEvent);
            Type[] foundEvents = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Namespace == targetNamespace && t.IsSubclassOf(baseEventType) && !t.IsInterface && !t.IsAbstract)
                .ToArray();

            int initializedEventsCounter = 0;
            foreach (Type t in foundEvents)
            {
                try
                {
                    BackgroundEvent backgroundEvent = (BackgroundEvent)Activator.CreateInstance(t)!;
                    Events[backgroundEvent.EvName] = backgroundEvent;
                    initializedEventsCounter++;
                }
                catch
                {
                }
            }
            return (initializedEventsCounter, foundEvents.Length);
        }

        public static BackgroundEvent? GetEvent(string name)
        {
            return Events.TryGetValue(name, out BackgroundEvent? backgroundEvent) ? backgroundEvent : null;
        }
    }
}
