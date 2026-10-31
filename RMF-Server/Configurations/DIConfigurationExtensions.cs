using Microsoft.Extensions.DependencyInjection;
using RMF.Core.Extensions;
using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal static class DIConfigurationExtensions
    {
        public static IServiceCollection AddSingletonConfigurations(this IServiceCollection services)
        {
            services.AddSingletonXmlConfig<AppearanceConfig>();
            services.AddSingletonXmlConfig<ConnectionConfig>();
            services.AddSingletonXmlConfig<FirewallConfig>();
            services.AddSingletonXmlConfig<TlsConfig>();
            services.AddSingletonXmlConfig<ControllerConfig>();
            services.AddSingletonXmlConfig<ChannelConfig>();
            services.AddSingletonXmlConfig<StreamingConfig>();
            services.AddSingletonXmlConfig<CommandConfig>();
            services.AddSingletonXmlConfig<ListenerConfig>();
            services.AddSingletonXmlConfig<LoggingConfig>();
            return services;
        }
    }
}
