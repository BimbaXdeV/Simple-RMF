using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
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
        private readonly Dictionary<string, Type> _eventTypes;

        public EventFactory(Dictionary<string, Type> eventTypes)
        {
            this._eventTypes = eventTypes;
        }

        public IEvent? CreateEvent(string eventName)
        {
            if (this._eventTypes.TryGetValue(eventName, out Type? backgroundEventType))
            {
                return Activator.CreateInstance(backgroundEventType) as BackgroundEvent;
            }
            return null;
        }

        public void ApplyEventSettings(string eventName, Dictionary<string, object> settings)
        {
            if (this._eventTypes.TryGetValue(eventName, out Type? backgroundEventType))
            {
                IEvent? backgroundEvent = Activator.CreateInstance(backgroundEventType) as BackgroundEvent;
                if (backgroundEvent != null)
                {
                    ApplyEventSettings(backgroundEvent, settings);
                }
            }
        }

        public void ApplyEventSettings(IEvent backgroundEvent, Dictionary<string, object> settings)
        {
            foreach (string key in settings.Keys)
            {
                PropertyInfo? prop = backgroundEvent.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        Type targetType = prop.PropertyType;
                        object rawValue = settings[key];
                        
                        object convertedValue = targetType.IsAssignableFrom(rawValue.GetType()) ? rawValue : Convert.ChangeType(rawValue, targetType);
                        prop.SetValue(backgroundEvent, convertedValue);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
