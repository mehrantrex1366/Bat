namespace Bat.EntityFrameworkCore;

public abstract class BatDbContext : DbContext, IBatDbContext
{
    protected BatDbContext() { }
    protected BatDbContext(DbContextOptions options) : base(options) { }
    protected BatDbContext(DbContextOptions<BatDbContext> options) : base(options) { }

    public virtual new DatabaseFacade Database => base.Database;
    public virtual new ChangeTracker ChangeTracker => base.ChangeTracker;
    
    // Writable public string properties per entity type. GetProperties() + LINQ used to run for every
    // tracked entity, twice, on every SaveChanges.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, System.Reflection.PropertyInfo[]> _stringProperties = new();

    private static System.Reflection.PropertyInfo[] GetStringProperties(Type type)
        => _stringProperties.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && x.CanWrite && x.PropertyType == typeof(string))
            .ToArray());

    // Note: like before, this visits every tracked entry (including Unchanged ones); values are only written when they change.
    public virtual void ApplyPersianYK()
    {
        foreach (var item in ChangeTracker.Entries())
        {
            if (item.Entity == null) continue;

            foreach (var property in GetStringProperties(item.Entity.GetType()))
            {
                if (property.GetValue(item.Entity) is string value)
                {
                    var newValue = value.ToPersianCharacters();
                    if (newValue != value) property.SetValue(item.Entity, newValue);
                }
            }
        }
    }
    public virtual void ApplyEnglishNumber()
    {
        // Only string properties can contain Persian/Arabic digits. int/long/float/double were also scanned
        // before, but their ToString() never contains them (and writing a string back into them would have thrown).
        foreach (var item in ChangeTracker.Entries())
        {
            if (item.Entity == null) continue;

            foreach (var property in GetStringProperties(item.Entity.GetType()))
            {
                if (property.GetValue(item.Entity) is string value)
                {
                    var newValue = value.ToEnglishNumber();
                    if (!ReferenceEquals(newValue, value) && newValue != value) property.SetValue(item.Entity, newValue);
                }
            }
        }
    }

    public virtual void PartialUpdate<TEntity>(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties) where TEntity : class, IBaseEntity
    {
        Entry(entity)?.State = EntityState.Detached;

        foreach (var prop in updatedProperties)
            Entry(entity).Property(prop).IsModified = true;
    }

    public override int SaveChanges()
    {
        ApplyPersianYK();
        ApplyEnglishNumber();

        this.BasePropertiesInitializer();

        return base.SaveChanges();
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyPersianYK();
        ApplyEnglishNumber();

        this.BasePropertiesInitializer();

        return await base.SaveChangesAsync(cancellationToken);
    }
    public virtual SaveChangeResult BatSaveChanges()
    {
        var result = new SaveChangeResult();
        try
        {
            ApplyPersianYK();
            ApplyEnglishNumber();

            this.BasePropertiesInitializer();

            result.Result = base.SaveChanges();
            result.IsSuccess = result.Result.ToSaveChangeResult();
            result.Message = result.Result.ToSaveChangeResultMessage(Strings.Success, Strings.UnknownException);
            result.ResultType = result.IsSuccess ? SaveChangeResultType.Success : SaveChangeResultType.UnknownException;

            return result;
        }
        catch (ValidationException validationException)
        {
            #region Validation Exception
            result.IsSuccess = false;
            result.Exception = validationException;
            result.ValidationErrors = this.ValidateContext();
            result.Message = Strings.EntityValidationException;
            result.ResultType = SaveChangeResultType.EntityValidationException;
            return result;
            #endregion
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            #region Concurrency Exception
            result.IsSuccess = false;
            result.Exception = concurrencyException;
            result.Message = Strings.UpdateConcurrencyException;
            result.ResultType = SaveChangeResultType.UpdateConcurrencyException;
            return result;
            #endregion
        }
        catch (DbUpdateException updateException)
        {
            #region Update Exception
            if ((updateException.InnerException is not null &&
                updateException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)) ||
                (updateException.InnerException is not null &&
                updateException.InnerException.InnerException is not null &&
                updateException.InnerException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)))
            {
                result.IsSuccess = false;
                result.Exception = updateException;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }

            result.IsSuccess = false;
            result.Exception = updateException;
            result.Message = Strings.UpdateException;
            result.ResultType = SaveChangeResultType.UpdateException;
            return result;
            #endregion
        }
        catch (Exception exception)
        {
            #region Public Exception
            if (exception.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase))
            {
                result.IsSuccess = false;
                result.Exception = exception;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }

            result.IsSuccess = false;
            result.Exception = exception;
            result.Message = Strings.UnknownException;
            result.ResultType = SaveChangeResultType.UnknownException;
            return result;
            #endregion
        }
    }
    public virtual async Task<SaveChangeResult> BatSaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = new SaveChangeResult();
        try
        {
            ApplyPersianYK();
            ApplyEnglishNumber();

            this.BasePropertiesInitializer();

            result.Result = await base.SaveChangesAsync(cancellationToken);
            result.IsSuccess = result.Result.ToSaveChangeResult();
            result.Message = result.Result.ToSaveChangeResultMessage(Strings.Success, Strings.UnknownException);
            result.ResultType = result.IsSuccess ? SaveChangeResultType.Success : SaveChangeResultType.UnknownException;

            return result;
        }
        catch (ValidationException validationException)
        {
            #region Validation Exception
            result.IsSuccess = false;
            result.Exception = validationException;
            result.ValidationErrors = this.ValidateContext();
            result.Message = Strings.EntityValidationException;
            result.ResultType = SaveChangeResultType.EntityValidationException;
            return result;
            #endregion
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            #region Concurrency Exception
            result.IsSuccess = false;
            result.Exception = concurrencyException;
            result.Message = Strings.UpdateConcurrencyException;
            result.ResultType = SaveChangeResultType.UpdateConcurrencyException;
            return result;
            #endregion
        }
        catch (DbUpdateException updateException)
        {
            #region Update Exception
            if ((updateException.InnerException is not null &&
                updateException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)) ||
                (updateException.InnerException is not null &&
                updateException.InnerException.InnerException is not null &&
                updateException.InnerException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)))
            {
                result.IsSuccess = false;
                result.Exception = updateException;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }

            result.IsSuccess = false;
            result.Exception = updateException;
            result.Message = Strings.UpdateException;
            result.ResultType = SaveChangeResultType.UpdateException;
            return result;
            #endregion
        }
        catch (Exception exception)
        {
            #region Public Exception
            if (exception.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase))
            {
                result.IsSuccess = false;
                result.Exception = exception;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }
            ;

            result.IsSuccess = false;
            result.Exception = exception;
            result.Message = Strings.UnknownException;
            result.ResultType = SaveChangeResultType.UnknownException;
            return result;
            #endregion
        }
    }
    public virtual SaveChangeResult BatSaveChangesWithValidation()
    {
        var result = new SaveChangeResult();
        try
        {
            var validationError = this.ValidateContext();
            if (validationError.Count > 0)
            {
                #region Validation Exception
                result.IsSuccess = false;
                result.ValidationErrors = validationError;
                result.Exception = new ValidationException();
                result.ResultType = SaveChangeResultType.EntityValidationException;
                return result;
                #endregion
            }

            ApplyPersianYK();
            ApplyEnglishNumber();

            this.BasePropertiesInitializer();

            result.Result = base.SaveChanges();
            result.IsSuccess = result.Result.ToSaveChangeResult();
            result.Message = result.Result.ToSaveChangeResultMessage(Strings.Success, Strings.UnknownException);
            result.ResultType = result.IsSuccess ? SaveChangeResultType.Success : SaveChangeResultType.UnknownException;

            return result;
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            #region Concurrency Exception
            result.IsSuccess = false;
            result.Exception = concurrencyException;
            result.Message = Strings.UpdateConcurrencyException;
            result.ResultType = SaveChangeResultType.UpdateConcurrencyException;
            return result;
            #endregion
        }
        catch (DbUpdateException updateException)
        {
            #region Update Exception
            if ((updateException.InnerException is not null &&
                updateException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)) ||
                (updateException.InnerException is not null &&
                updateException.InnerException.InnerException is not null &&
                updateException.InnerException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)))
            {
                result.IsSuccess = false;
                result.Exception = updateException;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }

            result.IsSuccess = false;
            result.Exception = updateException;
            result.Message = Strings.UpdateException;
            result.ResultType = SaveChangeResultType.UpdateException;
            return result;
            #endregion
        }
        catch (Exception exception)
        {
            #region Public Exception
            if (exception.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase))
            {
                result.IsSuccess = false;
                result.Exception = exception;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }
            ;

            result.IsSuccess = false;
            result.Exception = exception;
            result.Message = Strings.UnknownException;
            result.ResultType = SaveChangeResultType.UnknownException;
            return result;
            #endregion
        }
    }
    public virtual async Task<SaveChangeResult> BatSaveChangesWithValidationAsync(CancellationToken cancellationToken = default)
    {
        var result = new SaveChangeResult();
        try
        {
            var validationError = this.ValidateContext();
            if (validationError.Count > 0)
            {
                #region Validation Exception
                result.IsSuccess = false;
                result.ValidationErrors = validationError;
                result.Exception = new ValidationException();
                result.ResultType = SaveChangeResultType.EntityValidationException;
                return result;
                #endregion
            }

            ApplyPersianYK();
            ApplyEnglishNumber();

            this.BasePropertiesInitializer();

            result.Result = await base.SaveChangesAsync(cancellationToken);
            result.IsSuccess = result.Result.ToSaveChangeResult();
            result.Message = result.Result.ToSaveChangeResultMessage(Strings.Success, Strings.UnknownException);
            result.ResultType = result.IsSuccess ? SaveChangeResultType.Success : SaveChangeResultType.UnknownException;

            return result;
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            #region Concurrency Exception
            result.IsSuccess = false;
            result.Exception = concurrencyException;
            result.Message = Strings.UpdateConcurrencyException;
            result.ResultType = SaveChangeResultType.UpdateConcurrencyException;
            return result;
            #endregion
        }
        catch (DbUpdateException updateException)
        {
            #region Update Exception
            if ((updateException.InnerException is not null &&
                updateException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)) ||
                (updateException.InnerException is not null &&
                updateException.InnerException.InnerException is not null &&
                updateException.InnerException.InnerException.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase)))
            {
                result.IsSuccess = false;
                result.Exception = updateException;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }

            result.IsSuccess = false;
            result.Exception = updateException;
            result.Message = Strings.UpdateException;
            result.ResultType = SaveChangeResultType.UpdateException;
            return result;
            #endregion
        }
        catch (Exception exception)
        {
            #region Public Exception
            if (exception.Message.Contains("cannot insert duplicate key", StringComparison.CurrentCultureIgnoreCase))
            {
                result.IsSuccess = false;
                result.Exception = exception;
                result.Message = Strings.DuplicateIndexKeyException;
                result.ResultType = SaveChangeResultType.DuplicateIndexKeyException;
                return result;
            }
            ;

            result.IsSuccess = false;
            result.Exception = exception;
            result.Message = Strings.UnknownException;
            result.ResultType = SaveChangeResultType.UnknownException;
            return result;
            #endregion
        }
    }
}