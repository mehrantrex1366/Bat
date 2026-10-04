using Microsoft.Extensions.DependencyInjection;

namespace Bat.Di;

public static class MicrosoftIocExtensions
{
    public static IServiceCollection AddBatDynamicTransient(this IServiceCollection services, Assembly assembly)
    {
        var types = InjectableTypeScanner.GetTypes<ITransientInjection>();

        foreach (var type in types)
        {
            try
            {
                var typeInterface = InjectableTypeScanner.GetServiceInterface(type);
                if (type.IsGenericType)
                {
                    if (typeInterface == null) continue;
                    services.AddTransient(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition());
                }
                else
                {
                    if (typeInterface != null)
                        services.AddTransient(typeInterface, type);
                    else
                        services.AddTransient(type);
                }
            }
            catch { }
        }

        return services;
    }

    public static IServiceCollection AddBatDynamicScoped(this IServiceCollection services, Assembly assembly)
    {
        var types = InjectableTypeScanner.GetTypes<IScopedInjection>();

        foreach (var type in types)
        {
            try
            {
                var typeInterface = InjectableTypeScanner.GetServiceInterface(type);
                if (type.IsGenericType)
                {
                    if (typeInterface == null) continue;
                    services.AddScoped(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition());
                }
                else
                {
                    if (typeInterface != null)
                        services.AddScoped(typeInterface, type);
                    else
                        services.AddScoped(type);
                }
            }
            catch { }
        }

        return services;
    }

    public static IServiceCollection AddBatDynamicSingleton(this IServiceCollection services, Assembly assembly)
    {
        var types = InjectableTypeScanner.GetTypes<ISingletonInjection>();

        foreach (var type in types)
        {
            try
            {
                var typeInterface = InjectableTypeScanner.GetServiceInterface(type);
                if (type.IsGenericType)
                {
                    if (typeInterface == null) continue;
                    services.AddSingleton(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition());
                }
                else
                {
                    if (typeInterface != null)
                        services.AddSingleton(typeInterface, type);
                    else
                        services.AddSingleton(type);
                }
            }
            catch { }
        }

        return services;
    }
}