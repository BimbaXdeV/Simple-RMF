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
        private static readonly string NamespaceFormat = "{0}.Events.{1}";
        public static readonly Dictionary<string, Type> EventTypes = [];

        public static (int, int) RegisterFound(string category)
        {
            Assembly? executingAssembly = Assembly.GetExecutingAssembly();
            string? projectName = executingAssembly?.GetName().Name;
            if (executingAssembly == null || string.IsNullOrEmpty(projectName))
            {
                return (0, 0);
            }

            category = char.ToUpper(category[0]) + category.Substring(1).ToLower();  // You can enter the name in any case
            string targetNamespace = string.Format(NamespaceFormat, projectName, category);

            Type baseEventType = typeof(BackgroundEvent);
            Type[] foundEvents = executingAssembly
                .GetTypes()
                .Where(t => t.Namespace == targetNamespace && t.IsSubclassOf(baseEventType) && !t.IsInterface && !t.IsAbstract)
                .ToArray();

            int initializedEventsCounter = 0;
            foreach (Type t in foundEvents)
            {
                try
                {
                    EventTypes[t.Name] = t;
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
            if (EventTypes.TryGetValue(name, out Type? backgroundEvent))
            {
                return Activator.CreateInstance(backgroundEvent) as BackgroundEvent;
            }
            return null;
        }

        public static void ApplyEventSettings(BackgroundEvent backgroundEvent, Dictionary<string, object> settings)
        {
            foreach (string key in settings.Keys)
            {
                PropertyInfo? field = backgroundEvent.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null && field.CanWrite)
                {
                    try
                    {
                        object? convertedValue = Convert.ChangeType(settings[key], field.PropertyType);
                        field.SetValue(backgroundEvent, convertedValue);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
