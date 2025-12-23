namespace Bat.Test;

public class EfBogusSeeder
{
    private readonly DbContext _context;

    public EfBogusSeeder(DbContext context) => _context = context;

    public TSeed SeedEntity<TSeed>(
        int count = 1,
        Func<GeneralBogusBuilder<TSeed>, GeneralBogusBuilder<TSeed>> builderConfig = null
    ) where TSeed : class
    {
        var builder = new GeneralBogusBuilder<TSeed>();
        if (builderConfig != null) builder = builderConfig(builder);

        var entity = count == 1 ? builder.Generate() : builder.Generate(count).First();

        SetShadowProperties(entity);
        SetBackReferences(entity);

        _context.Add(entity);
        _context.SaveChanges();

        return entity;
    }

    public List<TSeed> SeedEntities<TSeed>(
        int count,
        Func<GeneralBogusBuilder<TSeed>, GeneralBogusBuilder<TSeed>> builderConfig = null
    ) where TSeed : class
    {
        var builder = new GeneralBogusBuilder<TSeed>();
        if (builderConfig != null) builder = builderConfig(builder);

        var entities = builder.Generate(count);

        foreach (var entity in entities)
        {
            SetShadowProperties(entity);
            SetBackReferences(entity);
            _context.Add(entity);
        }

        _context.SaveChanges();
        return entities;
    }

    public void SeedAll(int count = 1)
    {
        var entityTypes = _context.Model.GetEntityTypes()
            .Where(t => !t.IsOwned())
            .Select(t => t.ClrType)
            .ToList();

        foreach (var type in entityTypes)
        {
            SeedType(type, count);
        }

        _context.SaveChanges();
    }

    private void SeedType(Type type, int count)
    {
        var builderType = typeof(GeneralBogusBuilder<>).MakeGenericType(type);
        var builder = Activator.CreateInstance(builderType);
        var generateMany = builderType.GetMethod("Generate", new[] { typeof(int) })!;
        var entities = (IEnumerable<object>)generateMany.Invoke(builder, new object[] { count })!;

        foreach (var entity in entities)
        {
            SetShadowProperties(entity);
            SetOwnedEntities(entity);
            SetBackReferences(entity);
            _context.Add(entity);
        }
    }

    private void SetShadowProperties(object entity)
    {
        var entry = _context.Entry(entity);
        var shadowProps = entry.Metadata.GetProperties().Where(p => p.IsShadowProperty());

        foreach (var prop in shadowProps)
        {
            if (prop.ClrType == typeof(DateTime))
                entry.Property(prop.Name).CurrentValue = DateTime.UtcNow;
            else if (prop.ClrType == typeof(int))
                entry.Property(prop.Name).CurrentValue = 1;
            else if (prop.ClrType == typeof(Guid))
                entry.Property(prop.Name).CurrentValue = Guid.NewGuid();
        }
    }

    private void SetOwnedEntities(object entity)
    {
        var entry = _context.Entry(entity);

        var ownedNavigations = entry.Metadata
            .GetNavigations()
            .Where(n => n.TargetEntityType.IsOwned());

        foreach (var navigation in ownedNavigations)
        {
            if (navigation.IsCollection)
            {
                var collectionEntry = entry.Collection(navigation.Name);

                // 🔑 Ensure EF initializes collection
                collectionEntry.Load();

                var collection = collectionEntry.CurrentValue;
                if (collection == null)
                    continue;

                var elementType = navigation.TargetEntityType.ClrType;
                var ownedInstance = Activator.CreateInstance(elementType)!;

                // 🔑 Add via ICollection<T>
                var addMethod = collection.GetType().GetMethod("Add");
                addMethod?.Invoke(collection, new[] { ownedInstance });

                // Recursive
                SetOwnedEntities(ownedInstance);
            }
            else
            {
                var referenceEntry = entry.Reference(navigation.Name);

                if (referenceEntry.CurrentValue != null)
                    continue;

                var ownedInstance = Activator.CreateInstance(navigation.TargetEntityType.ClrType)!;
                referenceEntry.CurrentValue = ownedInstance;

                SetOwnedEntities(ownedInstance);
            }
        }
    }

    private void SetBackReferences(object entity)
    {
        var entry = _context.Entry(entity);
        var navProps = entry.Metadata.GetNavigations();

        foreach (var nav in navProps)
        {
            if (nav.IsCollection)
            {
                var collection = entry.Collection(nav.Name).CurrentValue;
                if (collection == null) continue;

                foreach (var child in (IEnumerable<object>)collection)
                {
                    var childEntry = _context.Entry(child);
                    var inverse = nav.Inverse;
                    if (inverse != null && childEntry.Property(inverse.Name).CurrentValue == null)
                        childEntry.Property(inverse.Name).CurrentValue = entity;

                    SetBackReferences(child);
                }
            }
            else
            {
                var refValue = entry.Reference(nav.Name).CurrentValue;
                if (refValue == null) continue;

                var inverse = nav.Inverse;
                if (inverse != null)
                {
                    var refEntry = _context.Entry(refValue);
                    if (nav.IsCollection)
                        refEntry.Collection(inverse.Name).CurrentValue = new List<object> { entity };
                    else
                        refEntry.Reference(inverse.Name).CurrentValue = entity;

                    SetBackReferences(refValue);
                }
            }
        }
    }
}