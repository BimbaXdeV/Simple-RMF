using Microsoft.Extensions.DependencyInjection;
using RMF.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal static class DIConfigurationExtensions
    {
        public static IServiceCollection AddSingletonConfigurations(this IServiceCollection services)
        {
            services.AddSingletonXmlConfig<AppearanceConfig>();
            services.AddSingletonXmlConfig<CaptureConfig>();
            services.AddSingletonXmlConfig<ChannelConfig>();
            services.AddSingletonXmlConfig<ConnectionConfig>();
            services.AddSingletonXmlConfig<ControllerConfig>();
            services.AddSingletonXmlConfig<SecurityConfig>();
            return services;
        }
    }
}
