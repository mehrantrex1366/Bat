# Bat.Di

`src/AspNetCore/Bat.Di` · namespace `Bat.Di` · depends on Bat.Core, Autofac.Extensions.DependencyInjection, DryIoc.

Auto-registers classes that implement Bat.Core marker interfaces:

| Container | Methods |
|---|---|
| Microsoft DI (`IServiceCollection`) | `AddBatDynamicTransient(assembly)`, `AddBatDynamicScoped(assembly)`, `AddBatDynamicSingleton(assembly)` |
| Autofac (`ContainerBuilder`) | `AddBatAutofacDynamicTransient/Scoped/Singleton(assembly)` |
| DryIoc (`IRegistrator`) | `AddBatDryIocDynamicTransient/Scoped/Singleton(params assemblies)` |

Rules (shared in `InjectableTypeScanner`):
- The `assembly` argument is **ignored** (historical); all assemblies loaded in the AppDomain at call time are scanned,
  skipping dynamic assemblies and assemblies whose types cannot be loaded. Make sure your assemblies are loaded
  (referenced and touched) before calling, e.g. call after `AddControllers()` or reference a type from each assembly.
- Candidates: public, non-abstract classes implementing `ITransientInjection` / `IScopedInjection` / `ISingletonInjection`.
- Service type: first implemented interface whose **name contains the class name** (`UserService` → `IUserService`);
  otherwise the class itself. Generic classes are registered as open generics (only when an interface matched).
- Registration errors for a single type are swallowed (the type is skipped).

Call it once at startup; scanning all assemblies costs startup time, not request time.
