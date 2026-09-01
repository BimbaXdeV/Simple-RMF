using Microsoft.Extensions.DependencyInjection;
using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSingletonXmlConfig<TConfig>(this IServiceCollection services) where TConfig : class, new()
        {
            return services.AddSingleton(provider =>
            {
                XmlConfigProvider configProvider = provider.GetRequiredService<XmlConfigProvider>();
                return configProvider.GetConfig<TConfig>();
            });
        }
    }
}
