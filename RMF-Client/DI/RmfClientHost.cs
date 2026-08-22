using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.DI
{
    internal class RmfClientHost
    {
        private readonly IHost _host;

        public RmfClientHost(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // To be implemented
            });

            this._host = builder.Build();
        }

        public async Task RunAsync()
        {
            await this._host.RunAsync();
        }
    }
}
