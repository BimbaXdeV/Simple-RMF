using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public abstract class BackgroundEvent
    {
        public abstract string EvName { get; }
        public bool IsEvRunning { get; private set; } = false;

        protected abstract Task BackgroundEvLogic(Stream stream, CancellationToken token);
        
        public async Task ExecuteEvAsync(Stream stream, CancellationToken token)
        {
            this.IsEvRunning = true;
            try
            {
                await BackgroundEvLogic(stream, token);
            }
            catch (Exception)
            {
            }
            finally
            {
                this.IsEvRunning = false;
            }
        }
    }
}
