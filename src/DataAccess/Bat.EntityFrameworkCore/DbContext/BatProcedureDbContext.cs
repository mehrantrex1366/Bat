namespace Bat.EntityFrameworkCore;

public class BatProcedureDbContext<TResult>(string connectionStrings) : DbContext where TResult : class
{
    private readonly string _connectionStrings = connectionStrings;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TResult>().HasNoKey();
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionStrings,
            sqlServerOption =>
            {
                //sqlServerOption.MaxBatchSize(50);
                sqlServerOption.CommandTimeout(null);
                sqlServerOption.UseRelationalNulls(true);
            });
    }

    public virtual DbSet<TResult> ResultSet { get; set; }
}