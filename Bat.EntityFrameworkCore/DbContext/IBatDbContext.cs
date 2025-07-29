namespace Bat.EntityFrameworkCore;

public interface IBatDbContext : IDisposable, IAsyncDisposable
{
    void ApplyPersianYK();
    void ApplyEnglishNumber();

    void PartialUpdate<TEntity>(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties) where TEntity : class, IBaseEntity;

    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    SaveChangeResult BatSaveChanges();
    Task<SaveChangeResult> BatSaveChangesAsync(CancellationToken cancellationToken = default);
    SaveChangeResult BatSaveChangesWithValidation();
    Task<SaveChangeResult> BatSaveChangesWithValidationAsync(CancellationToken cancellationToken = default);
}