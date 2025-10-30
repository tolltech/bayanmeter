using Microsoft.EntityFrameworkCore;
using Tolltech.PostgreEF.Integration;

namespace Tolltech.Planny;

public interface IDataContextFactory
{
    Task<PlannyContext> CreateDbContextAsync();
}

public class NpgSqlDataContextFactory : IDataContextFactory 
{
    private readonly DbContextOptions<PlannyContext> _options;

    public NpgSqlDataContextFactory(IConnectionString connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlannyContext>();
        optionsBuilder.UseNpgsql(connectionString.Value);
        _options = optionsBuilder.Options;
    }

    private PlannyContext CreateDbContext()
    {
        return new PlannyContext(_options);
    }

    public async Task<PlannyContext> CreateDbContextAsync()
    {
        return await Task.FromResult(CreateDbContext());
    }
}