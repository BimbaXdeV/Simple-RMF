using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal static class DICommandExtensions
    {
        public static IServiceCollection AddSingletonInlineCommands(this IServiceCollection services)
        {
            // Performance Commands
            services.AddSingleton<IExecutableCommand, HelpCommand>();
            services.AddSingleton<IExecutableCommand, ConnectionCommand>();
            services.AddSingleton<IExecutableCommand, BlacklistCommand>();
            services.AddSingleton<IExecutableCommand, ServerStatusCommand>();
            services.AddSingleton<IExecutableCommand, TlsCertificateCommand>();
            services.AddSingleton<IExecutableCommand, RmfVersionCommand>();
            services.AddSingleton<IExecutableCommand, ClearConsoleCommand>();
            services.AddSingleton<IExecutableCommand, ShutdownCommand>();

            // Management Commands
            // In process...
            return services;
        }
    }
}
