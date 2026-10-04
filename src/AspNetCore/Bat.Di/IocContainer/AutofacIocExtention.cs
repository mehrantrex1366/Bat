using Autofac;

namespace Bat.Di;

public static class AutofacIocExtensions
{
    public static ContainerBuilder AddBatAutofacDynamicTransient(this ContainerBuilder container, Assembly assembly)
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
                    container.RegisterGeneric(type).As(typeInterface).InstancePerDependency();
                }
                else
                {
                    if (typeInterface != null)
                        container.RegisterType(type).As(typeInterface).InstancePerDependency();
                    else
                        container.RegisterType(type).InstancePerDependency();
                }
            }
            catch { }
        }

        return container;
    }

    public static ContainerBuilder AddBatAutofacDynamicScoped(this ContainerBuilder container, Assembly assembly)
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
                    container.RegisterGeneric(type).As(typeInterface).InstancePerLifetimeScope();
                }
                else
                {
                    if (typeInterface != null)
                        container.RegisterType(type).As(typeInterface).InstancePerLifetimeScope();
                    else
                        container.RegisterType(type).InstancePerLifetimeScope();
                }
            }
            catch { }
        }

        return container;
    }

    public static ContainerBuilder AddBatAutofacDynamicSingleton(this ContainerBuilder container, Assembly assembly)
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
                    container.RegisterGeneric(type).As(typeInterface).SingleInstance();
                }
                else
                {
                    if (typeInterface != null)
                        container.RegisterType(type).As(typeInterface).SingleInstance();
                    else
                        container.RegisterType(type).SingleInstance();
                }
            }
            catch { }
        }

        return container;
    }
}