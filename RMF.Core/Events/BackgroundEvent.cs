using RMF.Core.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public abstract class BackgroundEvent
    {
        public bool IsEvRunning { get; private set; } = false;

        protected abstract Task HandleLogic(ClientSession session, CancellationToken token);
        
        public async Task ExecuteEvAsync(ClientSession session, CancellationToken token)
        {
            this.IsEvRunning = true;
            try
            {
                await Task.Delay(1000, token);  // Small delay (1sec) to ensure the event is fully registered before execution
                await HandleLogic(session, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while executing the background event: {ex}");
            }
            finally
            {
                this.IsEvRunning = false;
            }
        }
    }
}
