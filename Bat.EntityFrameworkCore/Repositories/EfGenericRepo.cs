namespace Bat.EntityFrameworkCore;

public class EFGenericRepo<TEntity>(DbContext context) where TEntity : class, IBaseEntity
{
    private readonly DbContext _context = context;
    public DbSet<TEntity> _dbSet = context.Set<TEntity>();


    public virtual void Add(TEntity model)
        => _dbSet.Add(model);

    public virtual async Task AddAsync(TEntity model, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(model, cancellationToken);

    public virtual void AddRange(IEnumerable<TEntity> models)
        => _dbSet.AddRange(models);

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> models, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(models, cancellationToken);

    public virtual void Update(TEntity model)
        => _dbSet.Update(model);

    public virtual void PartialUpdate(TEntity entity, List<string> updatedProperties)
    {
        foreach (var property in updatedProperties)
            _context.Entry(entity).Property(property).IsModified = true;
    }

    public virtual void PartialUpdate(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties)
    {
        if (_context.Entry(entity) is not null)
            _context.Entry(entity).State = EntityState.Detached;

        foreach (var property in updatedProperties)
            _context.Entry(entity).Property(property).IsModified = true;
    }

    public virtual void UpdateRange(IEnumerable<TEntity> models)
        => _dbSet.UpdateRange(models);

    public virtual async Task UpdateRangeAsync(Action<UpdateSettersBuilder<TEntity>> setProperties, CancellationToken cancellationToken = default)
  => await _dbSet.ExecuteUpdateAsync(setProperties, cancellationToken);

    public virtual void Delete(TEntity model)
        => _dbSet.Remove(model);

    public virtual void DeleteUnAttached(TEntity model)
    {
        _dbSet.Attach(model);
        _dbSet.Remove(model);
    }

    public virtual void DeleteRange(IEnumerable<TEntity> models)
        => _dbSet.RemoveRange(models);

    public virtual async Task DeleteRange(Expression<Func<TEntity, bool>> Conditions, CancellationToken cancellationToken = default)
        => await _dbSet.Where(Conditions).ExecuteDeleteAsync(cancellationToken);


    public virtual async Task<TEntity> FindAsync(object id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public virtual async Task<bool> AnyAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.AnyAsync();

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        return await query.AnyAsync(model.CancellationToken);
    }

    public virtual async Task<int> CountAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.CountAsync();

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        return await query.CountAsync(model.CancellationToken);
    }

    public virtual async Task<long> LongCountAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.LongCountAsync();

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        return await query.LongCountAsync(model.CancellationToken);
    }

    public virtual async Task<TEntity> FirstOrDefaultAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.FirstOrDefaultAsync();

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        return await query.FirstOrDefaultAsync(model.CancellationToken);
    }

    public virtual async Task<TResult> FirstOrDefaultAsync<TResult>(QueryFilterWithSelector<TEntity, TResult> model)
    {
        if (model.Selector.IsNull()) throw new Exception("Selector Not Assigned");

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        return await query.Select(model.Selector).FirstOrDefaultAsync(model.CancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.ToListAsync();

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        if (model.PagingParameter is not null) query = query.Skip((model.PagingParameter.PageNumber - 1) * model.PagingParameter.PageSize).Take(model.PagingParameter.PageSize);
        return await query.ToListAsync(model.CancellationToken);
    }

    public virtual async Task<List<TResult>> GetAsync<TResult>(QueryFilterWithSelector<TEntity, TResult> model)
    {
        if (model.Selector.IsNull()) throw new Exception("Selector Not Assigned");

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        if (model.PagingParameter is not null) query = query.Skip((model.PagingParameter.PageNumber - 1) * model.PagingParameter.PageSize).Take(model.PagingParameter.PageSize);
        return await query.Select(model.Selector).ToListAsync(model.CancellationToken);
    }

    public virtual async Task<PagingListDetails<TEntity>> GetPagingAsync(QueryFilter<TEntity> model = null)
    {
        if (model.IsNull()) return await _dbSet.ToPagingListDetailsAsync(new PagingParameter { PageNumber = 1, PageSize = 10 });

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        return await query.ToPagingListDetailsAsync(model.PagingParameter ?? new PagingParameter { PageNumber = 1, PageSize = 10 }, model.CancellationToken);
    }

    public virtual async Task<PagingListDetails<TResult>> GetPagingAsync<TResult>(QueryFilterWithSelector<TEntity, TResult> model)
    {
        if (model.Selector.IsNull()) throw new Exception("Selector Not Assigned");

        IQueryable<TEntity> query = model.AsNoTracking ? _dbSet.AsNoTracking() : _dbSet.AsQueryable();
        if (model.Conditions is not null) query = query.Where(model.Conditions);
        model.IncludeProperties?.ForEach(i => { query = query.Include(i); });
        if (model.ThenIncludeProperties is not null) query = model.ThenIncludeProperties(query);
        if (model.OrderBy is not null) query = model.OrderBy(query);
        return await query.Select(model.Selector).ToPagingListDetailsAsync(model.PagingParameter ?? new PagingParameter { PageNumber = 1, PageSize = 10 }, model.CancellationToken);
    }


    public virtual async Task<List<TEntity>> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default, params object[] parameters)
        => await _dbSet.FromSqlRaw(sql, parameters).ToListAsync(cancellationToken);
}