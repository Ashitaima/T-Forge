using TForge.Common;
using TForge.Data.Context;
using TForge.Services;
using TForge.Services.Interfaces;
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
            IRatingService ratingService,
            ILogger logger)
        {
            await ApplyMigrationsAsync(context, logger);
            await NormalizeLegacyStatusesAsync(context, logger);
            await NormalizeLegacyCountriesAsync(context, logger);
            await DbSeeder.SeedAsync(context, passwordHasher);

            // Драбина програється тим самим калькулятором, що й жива гра, тож
            // у перший же день видно справжній розклад сил, а не всіх, рівних
            // базовому рейтингу. Виклик ідемпотентний: уже враховані матчі
            // відсіює журнал.
            await ratingService.BackfillAsync();
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

        /// <summary>
        /// Країна тепер зберігається кодом ISO — з нього виводиться прапор.
        /// Профілі, створені до цього, тримають назву англійською; перекладаємо
        /// відомі назви один раз, щоб їхній прапор з'явився сам, а не після
        /// того, як власник відкриє форму. Невідомі значення лишаємо як є:
        /// стерти чужі дані гірше, ніж показати їх без прапора.
        /// </summary>
        private static async Task NormalizeLegacyCountriesAsync(EsportsDbContext context, ILogger logger)
        {
            var stored = await context.Players
                .Where(p => p.Country != "")
                .Select(p => p.Country)
                .Distinct()
                .ToListAsync();

            var replacements = stored
                .Where(value => !Countries.IsValid(value))
                .Select(value => new { From = value, To = Countries.ToCode(value) })
                .Where(pair => pair.To != null)
                .ToList();

            var updated = 0;
            foreach (var pair in replacements)
            {
                updated += await context.Players
                    .Where(p => p.Country == pair.From)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Country, pair.To));
            }

            if (updated > 0)
            {
                logger.LogInformation("Країни переведено на коди ISO: оновлено профілів — {Players}", updated);
            }
        }
    }
}
