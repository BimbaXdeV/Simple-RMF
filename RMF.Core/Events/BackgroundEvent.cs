using RMF.Core.Bases;
using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public abstract class BackgroundEvent : IEvent
    {
        public bool IsEvRunning { get; private set; } = false;

        protected abstract Task HandleLogic(ClientSession session, CancellationToken token);
        
        public async Task ExecuteAsync(ClientSession session, CancellationToken token)
        {
            this.IsEvRunning = true;
            try
            {
                await Task.Delay(1000, token);  // Small delay (1sec) to ensure the event is fully registered before execution
                await HandleLogic(session, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                this.IsEvRunning = false;
            }
        }
    }
}
