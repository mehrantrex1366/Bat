using DryIoc;

namespace Bat.Di;

public static class DryIocContainerExtensions
{
    public static void AddBatDryIocDynamicTransient(this IRegistrator container, params Assembly[] assemblies)
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
                    container.Register(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition(), Reuse.Transient);
                }
                else
                {
                    if (typeInterface != null)
                        container.Register(typeInterface, type, Reuse.Transient);
                    else
                        container.Register(type, Reuse.Transient);
                }
            }
            catch { }
        }
    }

    public static void AddBatDryIocDynamicScoped(this IRegistrator container, params Assembly[] assemblies)
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
                    container.Register(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition(), Reuse.Scoped);
                }
                else
                {
                    if (typeInterface != null)
                        container.Register(typeInterface, type, Reuse.Scoped);
                    else
                        container.Register(type, Reuse.Scoped);
                }
            }
            catch { }
        }
    }

    public static void AddBatDryIocDynamicSingleton(this IRegistrator container, params Assembly[] assemblies)
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
                    container.Register(typeInterface.GetGenericTypeDefinition(), type.GetGenericTypeDefinition(), Reuse.Singleton);
                }
                else
                {
                    if (typeInterface != null)
                        container.Register(typeInterface, type, Reuse.Singleton);
                    else
                        container.Register(type, Reuse.Singleton);
                }
            }
            catch { }
        }
    }
}