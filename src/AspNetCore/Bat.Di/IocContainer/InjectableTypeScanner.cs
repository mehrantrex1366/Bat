namespace Bat.Di;

/// <summary>
/// Shared type scanning for the Autofac / DryIoc / Microsoft DI auto-registration extensions.
/// </summary>
/// <remarks>
/// Same selection rules as before (every loaded assembly, public non-abstract classes implementing the marker
/// interface; the service interface is the first implemented interface whose name contains the class name), with fixes:
/// <list type="bullet">
/// <item>dynamic assemblies (proxies, Reflection.Emit) are skipped: GetExportedTypes() throws NotSupportedException
/// for them, which used to abort the whole registration at startup;</item>
/// <item>an assembly that fails to load its types no longer aborts the scan;</item>
/// <item>the redundant Assembly.Load(FullName) for already-loaded assemblies is removed.</item>
/// </list>
/// </remarks>
internal static class InjectableTypeScanner
{
    internal static List<Type> GetTypes<TMarker>()
    {
        var result = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;

            Type[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
                if (type.IsClass && !type.IsAbstract && type.IsPublic && typeof(TMarker).IsAssignableFrom(type))
                    result.Add(type);
        }

        return result;
    }

    internal static Type GetServiceInterface(Type type)
        => type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(x => x.Name.Contains(type.Name));
}