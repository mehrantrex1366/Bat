namespace Bat.EntityFrameworkCore;

public static class DbContextExtensions
{
    public async static Task<List<TResult>> ExecuteProcedure<TResult>(this DbContext dbContext, string sqlQuery, params object[] parameters) where TResult : class
    {
        using var procedureContext = new BatProcedureDbContext<TResult>(dbContext.Database.GetConnectionString());
        return await procedureContext.ResultSet.FromSqlRaw(sqlQuery, parameters).ToListAsync();
    }

    public async static Task<bool> ExecuteCommandAsync<TEntity>(this DbContext dbContext, string sql, params object[] parameters) where TEntity : class
       => await dbContext.Database.ExecuteSqlRawAsync(sql, parameters) >= 0;

    public static IQueryable<TEntity> ExecuteQuery<TEntity>(this DbContext dbContext, string sql, params object[] parameters) where TEntity : class
        => dbContext.Set<TEntity>().FromSqlRaw(sql, parameters);

    public async static Task<List<TEntity>> ExecuteQueryListAsync<TEntity>(this DbContext dbContext, string sql, params object[] parameters) where TEntity : class
        => await dbContext.Set<TEntity>().FromSqlRaw(sql, parameters).ToListAsync();


    public static bool ContainsEntity<TEntity>(this DbContext dbContext) where TEntity : class
        => dbContext.Model.FindEntityType(typeof(TEntity)) != null;

    public static IEnumerable<EntityEntry> GetAddOrUpdateEntity(this DbContext dbContext)
        => dbContext.ChangeTracker.Entries().Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

    // Fixed: GetAddedEntity and GetUpdatedEntity both returned the Deleted entries.
    public static IEnumerable<EntityEntry> GetAddedEntity(this DbContext dbContext)
        => dbContext.GetChangedEntity(EntityState.Added);

    public static IEnumerable<EntityEntry> GetUpdatedEntity(this DbContext dbContext)
        => dbContext.GetChangedEntity(EntityState.Modified);

    public static IEnumerable<EntityEntry> GetDeletedEntity(this DbContext dbContext)
        => dbContext.GetChangedEntity(EntityState.Deleted);

    public static IEnumerable<EntityEntry> GetChangedEntity(this DbContext dbContext, EntityState? entityState = null)
    {
        var entries = dbContext.ChangeTracker.Entries();
        if (entityState.HasValue) entries = entries.Where(x => x.State == entityState.Value);
        return entries;
    }

    public static void BasePropertiesInitializer(this DbContext dbContext)
    {
        // One timestamp per SaveChanges: all entities saved together get the same Insert/Modify time
        // (DateTime.Now was read up to 8 times per entity before, and the Persian date was formatted per entity).
        var now = DateTime.Now;
        string persianNow = null;
        string PersianNow() => persianNow ??= now.ToPersianDate();

        foreach (var entry in dbContext.ChangeTracker.Entries<IBaseProperties>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        if (entry.Entity is IInsertDateOnlyProperty insertDateOnlyProperty)
                            insertDateOnlyProperty.InsertDate = DateOnly.FromDateTime(now);

                        if (entry.Entity is IInsertTimeOnlyProperty insertTimeOnlyProperty)
                            insertTimeOnlyProperty.InsertTime = TimeOnly.FromDateTime(now);

                        if (entry.Entity is IInsertDateProperty insertDateProperty)
                            insertDateProperty.InsertDateMi = now;

                        if (entry.Entity is IInsertDateProperties insertDateProperties)
                        {
                            insertDateProperties.InsertDateMi = now;
                            insertDateProperties.InsertDateSh = PersianNow();
                        }


                        if (entry.Entity is IModifyDateOnlyProperty modifyDateOnlyProperty)
                            modifyDateOnlyProperty.ModifyDate = DateOnly.FromDateTime(now);

                        if (entry.Entity is IModifyTimeOnlyProperty modifyTimeOnlyProperty)
                            modifyTimeOnlyProperty.ModifyTime = TimeOnly.FromDateTime(now);

                        if (entry.Entity is IModifyDateProperty modifyDateProperty)
                            modifyDateProperty.ModifyDateMi = now;

                        if (entry.Entity is IModifyDateProperties modifyDateProperties)
                        {
                            modifyDateProperties.ModifyDateMi = now;
                            modifyDateProperties.ModifyDateSh = PersianNow();
                        }
                        break;
                    }
                case EntityState.Modified:
                    {
                        if (entry.Entity is IModifyDateOnlyProperty modifyDateOnlyProperty)
                            modifyDateOnlyProperty.ModifyDate = DateOnly.FromDateTime(now);

                        if (entry.Entity is IModifyTimeOnlyProperty modifyTimeOnlyProperty)
                            modifyTimeOnlyProperty.ModifyTime = TimeOnly.FromDateTime(now);
                        
                        if (entry.Entity is IModifyDateProperty modifyDateProperty)
                            modifyDateProperty.ModifyDateMi = now;
                        
                        if (entry.Entity is IModifyDateProperties modifyDateProperties)
                        {
                            modifyDateProperties.ModifyDateMi = now;
                            modifyDateProperties.ModifyDateSh = PersianNow();
                        }
                        break;
                    }
                case EntityState.Deleted:
                    {
                        if (entry.Entity is ISoftDeleteProperty softDeleteProperty)
                        {
                            softDeleteProperty.IsDeleted = true;
                            entry.State = EntityState.Modified;
                        }
                        break;
                    }
            }
        }
    }

    public static Dictionary<string, ValidationError> ValidateContext(this DbContext dbContext)
    {
        var result = new Dictionary<string, ValidationError>();
        foreach (var entity in dbContext.GetAddOrUpdateEntity().Select(x => x.Entity))
        {
            try
            {
                var validationContext = new ValidationContext(entity);
                Validator.ValidateObject(
                    entity,
                    validationContext,
                    validateAllProperties: true);
            }
            catch (ValidationException validationException)
            {
                // Fixed: Add() threw ArgumentException when two entities of the same type were invalid, and
                // Value/MemberNames could be null/empty. The first error per entity type is kept (key format unchanged).
                result.TryAdd(entity.GetType().FullName, new ValidationError
                {
                    Value = validationException.Value?.ToString(),
                    ValidationSource = validationException.Source,
                    Field = validationException.ValidationResult.MemberNames.FirstOrDefault(),
                    ValidationMessage = validationException.ValidationResult.ErrorMessage
                });
            }
        }
        return result;
    }

    public static void SaveAuditLog<TEntity>(this DbContext dbContext, string userId) where TEntity : class, new()
    {
        try
        {
            if (!dbContext.ContainsEntity<TEntity>()) return;

            var auditList = new List<TEntity>();
            var tableName = string.Empty;
            // Fixed: every tracked entry (including Unchanged ones and the audit rows themselves) used to produce an
            // audit row; Unchanged entries produced empty rows. Only Added/Modified/Deleted entries are audited now.
            // ToList(): the loop does not add entries, but GetDatabaseValues() must not run while enumerating the tracker.
            foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
                if (entry.Entity is TEntity) continue;

                tableName = entry.Entity.GetType().FullName;
                tableName = tableName.Contains('_') ? tableName[..tableName.IndexOf('_')] : tableName;
                var audit = new TEntity() as IAuditLogProperties;
                switch (entry.State)
                {
                    case EntityState.Added:
                        {
                            #region Added
                            audit.UserId = userId;
                            audit.ActionType = EntityState.Added.ToString();
                            audit.EntityName = tableName;
                            audit.NewValue = entry.CurrentValues.ToObject().SerializeDbSetToJson();
                            audit.OldValue = null;
                            audit.InsertDateMi = DateTime.Now;
                            audit.InsertDateSh = PersianDateTime.Now.ToString();
                            break;
                            #endregion
                        }
                    case EntityState.Modified:
                        {
                            #region Modified
                            audit.UserId = userId;
                            audit.ActionType = EntityState.Modified.ToString();
                            audit.EntityName = tableName;
                            audit.NewValue = entry.CurrentValues.ToObject().SerializeDbSetToJson();
                            audit.OldValue = entry.GetDatabaseValues().ToObject().SerializeDbSetToJson();
                            audit.InsertDateMi = DateTime.Now;
                            audit.InsertDateSh = PersianDateTime.Now.ToString();
                            break;
                            #endregion
                        }
                    case EntityState.Deleted:
                        {
                            #region Deleted
                            audit.UserId = userId;
                            audit.ActionType = EntityState.Deleted.ToString();
                            audit.EntityName = tableName;
                            audit.NewValue = null;
                            audit.OldValue = entry.OriginalValues.ToObject().SerializeDbSetToJson();
                            audit.InsertDateMi = DateTime.Now;
                            audit.InsertDateSh = PersianDateTime.Now.ToString();
                            break;
                            #endregion
                        }
                }
                auditList.Add(audit as TEntity);
            }
            dbContext.Set<TEntity>().AddRange(auditList);
        }
        catch { }
    }



    public static string GetConnectionString(this DbContext dbContext)
        => dbContext.Database.GetConnectionString();

    public static SqlConnection GetSqlConnection(this DbContext dbContext)
        => new(dbContext.Database.GetConnectionString());
}