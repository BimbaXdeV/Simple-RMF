using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public abstract class AddressedBackgroundEvent : BackgroundEvent
    {
        // A separate identifier for server events that are addressed to specific clients
        protected string IPAddress { get; set; } = string.Empty;
    }
}
