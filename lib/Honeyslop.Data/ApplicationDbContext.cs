using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data;

public sealed partial class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseNpgsql(builder =>
            {
                builder.MigrationsHistoryTable("__ef_migrations_history");
                builder.MigrationsAssembly("Honeyslop.Migrator");
            })
            .UseSnakeCaseNamingConvention();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Entry>()
            .OwnsMany(x => x.ResponseHeaders, builder => builder.ToJson());
    }
}
