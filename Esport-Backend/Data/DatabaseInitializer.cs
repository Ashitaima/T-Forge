using TForge.Common;
using TForge.Data.Context;
using TForge.Services;
using Microsoft.EntityFrameworkCore;

namespace TForge.Data
{
    /// <summary>
    /// Приводить базу даних до актуального стану на старті застосунку:
    /// застосовує міграції, нормалізує застарілі статуси й наповнює демо-даними.
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(
            EsportsDbContext context,
            IPasswordHasher passwordHasher,
            ILogger logger)
        {
            await ApplyMigrationsAsync(context, logger);
            await NormalizeLegacyStatusesAsync(context, logger);
            await DbSeeder.SeedAsync(context, passwordHasher);
        }

        private static async Task ApplyMigrationsAsync(EsportsDbContext context, ILogger logger)
        {
            // База, створена через EnsureCreated, не має історії міграцій — Migrate() у такому
            // разі впаде з "relation already exists". Даємо зрозумілу підказку замість цього.
            if (await context.Database.CanConnectAsync())
            {
                var applied = await context.Database.GetAppliedMigrationsAsync();
                if (!applied.Any() && await UsersTableExistsAsync(context))
                {
                    throw new InvalidOperationException(
                        "Базу даних було створено через EnsureCreated() і вона не має історії міграцій. " +
                        "Видаліть її один раз (dotnet ef database drop -f) та перезапустіть застосунок, " +
                        "щоб схему створили міграції.");
                }
            }

            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Застосовуємо міграції: {Migrations}", string.Join(", ", pending));
            }

            await context.Database.MigrateAsync();
        }

        private static async Task<bool> UsersTableExistsAsync(EsportsDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass('public.users') IS NOT NULL";

            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync();
            }

            try
            {
                var result = await command.ExecuteScalarAsync();
                return result is bool exists && exists;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Приводить статуси, збережені старими версіями коду, до канонічних значень.
        /// </summary>
        private static async Task NormalizeLegacyStatusesAsync(EsportsDbContext context, ILogger logger)
        {
            var fixedMatches = await context.Matches
                .Where(m => m.Status == "In Progress")
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.Status, MatchStatus.InProgress));

            var fixedTournaments = await context.Tournaments
                .Where(t => t.Status == "Active")
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Status, TournamentStatus.InProgress));

            if (fixedMatches > 0 || fixedTournaments > 0)
            {
                logger.LogInformation(
                    "Нормалізовано застарілі статуси: матчів — {Matches}, турнірів — {Tournaments}",
                    fixedMatches, fixedTournaments);
            }
        }
    }
}
