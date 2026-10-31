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
        public static IServiceCollection AddSingletonXmlConfig<T>(this IServiceCollection services) where T : class, IConfig, new()
        {
            return services.AddSingleton(provider =>
            {
                RmfConfigProvider configProvider = provider.GetRequiredService<RmfConfigProvider>();
                return configProvider.GetConfig<T>();
            });
        }
    }
}
