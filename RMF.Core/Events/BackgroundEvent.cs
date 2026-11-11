using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public abstract class BackgroundEvent : IEvent
    {
        public bool IsEvRunning { get; private set; } = false;

        protected abstract Task HandleLogic(ISession session, CancellationToken token);
        public abstract bool ValidateSettings(out string? exceptionMessage);
        public abstract void ApplySettings(IReadOnlyDictionary<string, object> settings);

        protected static void ApplyValue<T>(IReadOnlyDictionary<string, object> settings, string key, Action<T> setter)
        {
            if (settings.TryGetValue(key, out object? value) && value != null)
            {
                if (value is T typedValue)
                {
                    setter(typedValue);
                    return;
                }

                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter.CanConvertFrom(typeof(object)))
                {
                    T convertedValue = (T)converter.ConvertFrom(value)!;
                    setter(convertedValue);
                }
            }
        }

        public async Task ExecuteAsync(ISession session, CancellationToken token)
        {
            this.IsEvRunning = true;
            try
            {
                await HandleLogic(session, token);
            }
            finally
            {
                this.IsEvRunning = false;
            }
        }
    }
}
