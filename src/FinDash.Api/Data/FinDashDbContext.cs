using FinDash.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinDash.Api.Data;

public sealed class FinDashDbContext(DbContextOptions<FinDashDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<PriceAlertEntity> PriceAlerts => Set<PriceAlertEntity>();
    public DbSet<AlertEventEntity> AlertEvents => Set<AlertEventEntity>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // D9: montos exactos, decimal(18,8).
        builder.Properties<decimal>().HavePrecision(18, 8);

        // Enums como texto, igual que en el JSON.
        builder.Properties<Currency>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<AssetKind>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<TxType>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<AlertCondition>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<AlertState>().HaveConversion<string>().HaveMaxLength(16);

        // SQLite no sabe ordenar ni comparar DateTimeOffset: se guarda como número.
        // Al pasar a SQL Server se quita esta línea y se regeneran las migraciones.
        builder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DisplayName).HasMaxLength(100);
        });

        b.Entity<AssetEntity>(e =>
        {
            e.ToTable("Assets");
            e.Property(a => a.Id).HasMaxLength(50);
            e.Property(a => a.Symbol).HasMaxLength(16);
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.Source).HasMaxLength(16);
            e.HasData(SeedData.Assets);
        });

        b.Entity<WatchlistItem>(e =>
        {
            e.ToTable("WatchlistItems");
            e.HasKey(w => new { w.UserId, w.AssetId });
            e.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AssetEntity>().WithMany().HasForeignKey(w => w.AssetId);
        });

        b.Entity<TransactionEntity>(e =>
        {
            e.ToTable("Transactions");
            e.Property(t => t.Id).ValueGeneratedNever(); // lo genera la app (contrato §1)
            e.HasIndex(t => new { t.UserId, t.AssetId });
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AssetEntity>().WithMany().HasForeignKey(t => t.AssetId);
        });

        b.Entity<PriceAlertEntity>(e =>
        {
            e.ToTable("PriceAlerts");
            e.Property(a => a.Id).ValueGeneratedNever(); // lo genera la app (contrato §1)
            e.HasIndex(a => a.UserId);
            e.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AssetEntity>().WithMany().HasForeignKey(a => a.AssetId);
        });

        b.Entity<AlertEventEntity>(e =>
        {
            e.ToTable("AlertEvents");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.UserId, x.DeliveredAt });
            e.HasIndex(x => x.AlertId);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PriceSnapshot>(e =>
        {
            e.ToTable("PriceSnapshots");
            e.Property(s => s.Change24hPct).HasPrecision(9, 4);
            e.HasIndex(s => new { s.AssetId, s.CapturedAt }).IsDescending(false, true);
            e.HasOne<AssetEntity>().WithMany().HasForeignKey(s => s.AssetId);
        });
    }
}

/// <summary>Lo usa "dotnet ef" para crear migraciones sin arrancar la API.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinDashDbContext>
{
    public FinDashDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FinDashDbContext>().UseSqlite("Data Source=findash.db").Options);
}
