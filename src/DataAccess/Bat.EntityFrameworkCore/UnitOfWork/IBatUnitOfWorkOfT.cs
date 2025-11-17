namespace Bat.EntityFrameworkCore;

public interface IBatUnitOfWork<TContext> : IBatUnitOfWork, IDisposable, IAsyncDisposable where TContext : BatDbContext
{
    Task<int> SaveChangesAsync(params BatDbContext[] batDbContexts);
    Task<int> SaveChangesAsync(params IBatUnitOfWork[] unitOfWorks);
}