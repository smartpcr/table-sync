using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KustoTest2.Config
{
    public static class OptionsBuilder
    {
        public static IServiceCollection ConfigureSettings<T>(this IServiceCollection services) where T : class, new()
        {
            services.AddOptions<T>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(typeof(T).Name).Bind(settings);
                });
            return services;
        }

        public static T GetConfiguredSettings<T>(this IConfiguration configuration) where T : class, new()
        {
            T settings = new T();
            configuration.Bind(typeof(T).Name, settings);
            return settings;
        }
    }
}
