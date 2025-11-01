using Microsoft.EntityFrameworkCore;

namespace Tolltech.Planny;

public class PlannyContext(DbContextOptions<PlannyContext> options) : DbContext(options)
{
    public DbSet<PlanDbo> Plans { get; set; }
    public DbSet<ChatSettingsDbo> ChatSettings { get; set; }
}