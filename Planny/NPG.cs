using Microsoft.EntityFrameworkCore;
using Tolltech.PostgreEF.Integration;

namespace Tolltech.Planny;

public interface IDataContextFactory<TContext> where TContext : DbContext
{
    TContext CreateDbContext();
    Task<TContext> CreateDbContextAsync();
}

public class NpgSqlDataContextFactory<TContext> : IDataContextFactory<TContext> 
    where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options;

    public NpgSqlDataContextFactory(IConnectionString connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(connectionString.Value);
        _options = optionsBuilder.Options;
    }

    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), _options)!;
    }

    public async Task<TContext> CreateDbContextAsync()
    {
        return await Task.FromResult(CreateDbContext());
    }
}