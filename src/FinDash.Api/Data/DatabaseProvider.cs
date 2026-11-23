using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Data;

/// <summary>
/// Motor de base de datos según Database:Provider.
/// - SqlServer (por defecto): la base del proyecto (D14). En local, LocalDB.
/// - Sqlite: solo para las pruebas automáticas y el CI, que no tienen SQL Server.
/// </summary>
public static class DatabaseProvider
{
    public static bool IsSqlite(IConfiguration config) =>
        string.Equals(config["Database:Provider"]?.Trim(), "Sqlite", StringComparison.OrdinalIgnoreCase);

    public static DbContextOptionsBuilder UseConfiguredDatabase(this DbContextOptionsBuilder options, IConfiguration config)
    {
        var connection = config.GetConnectionString("Default");
        return IsSqlite(config) ? options.UseSqlite(connection) : options.UseSqlServer(connection);
    }

    /// <summary>
    /// SQL Server se actualiza con las migraciones. SQLite (pruebas) crea las tablas directo,
    /// porque las migraciones están escritas para SQL Server.
    /// </summary>
    public static Task PrepareAsync(FinDashDbContext db, CancellationToken ct = default) =>
        db.Database.IsSqlite() ? db.Database.EnsureCreatedAsync(ct) : db.Database.MigrateAsync(ct);
}
