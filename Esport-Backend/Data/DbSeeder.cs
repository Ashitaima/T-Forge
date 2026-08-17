using TForge.Common;
using TForge.Data.Context;
using TForge.Models;
using TForge.Services;
using Microsoft.EntityFrameworkCore;

namespace TForge.Data
{
    public static class DbSeeder
    {
        /// <summary>
        /// Пароль для всіх демо-акаунтів (admin, organizer1, player1..player4).
        /// Лише для локальної розробки.
        /// </summary>
        public const string DevPassword = "DevPassw0rd";

        public static async Task SeedAsync(EsportsDbContext context, IPasswordHasher passwordHasher)
        {
            // Схема створюється міграціями в DatabaseInitializer, а не EnsureCreated

            // Проверяем, есть ли уже данные
            if (await context.Users.AnyAsync())
            {
                await EnsureScheduledMatchesAsync(context);
                return; // База данных уже засеяна
            }

            // Создаем пользователей
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRoles.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "organizer1",
                    Email = "organizer1@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "John",
                    LastName = "Organizer",
                    Role = UserRoles.Organizer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player1",
                    Email = "player1@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "Alex",
                    LastName = "Gamer",
                    Role = UserRoles.Player,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player2",
                    Email = "player2@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "Sarah",
                    LastName = "Pro",
                    Role = UserRoles.Player,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player3",
                    Email = "player3@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "Mike",
                    LastName = "Elite",
                    Role = UserRoles.Player,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player4",
                    Email = "player4@esports.com",
                    PasswordHash = passwordHasher.Hash(DevPassword),
                    FirstName = "Emma",
                    LastName = "Champion",
                    Role = UserRoles.Player,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Создаем команды
            var teams = new List<Team>
            {
                new Team
                {
                    Name = "Phoenix Warriors",
                    Tag = "PHX",
                    Description = "Professional esports team specializing in FPS games",
                    CaptainId = users[2].Id, // player1
                    Region = "Europe",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Team
                {
                    Name = "Dragon Slayers",
                    Tag = "DRAG",
                    Description = "Elite team with focus on strategy games",
                    CaptainId = users[3].Id, // player2
                    Region = "North America",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Teams.AddRangeAsync(teams);
            await context.SaveChangesAsync();

            // Создаем игроков
            var players = new List<Player>
            {
                new Player
                {
                    UserId = users[2].Id, // player1
                    Nickname = "AlexPro",
                    Position = PlayerPositions.Rifler,
                    Country = "UA",
                    Age = 22,
                    TeamId = teams[0].Id, // Phoenix Warriors
                    // Кешовані лічильники мають відповідати рядкам MatchPlayer,
                    // що засіваються нижче для завершеного матчу Phoenix Warriors vs Dragon Slayers
                    TotalMatches = 1,
                    Wins = 1,
                    Losses = 0,
                    WinRate = 100m,
                    Ranking = 1250,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Player
                {
                    UserId = users[3].Id, // player2
                    Nickname = "SarahSniper",
                    Position = PlayerPositions.AWPer,
                    Country = "US",
                    Age = 24,
                    TeamId = teams[1].Id, // Dragon Slayers
                    // Кешовані лічильники мають відповідати рядкам MatchPlayer,
                    // що засіваються нижче для завершеного матчу Phoenix Warriors vs Dragon Slayers
                    TotalMatches = 1,
                    Wins = 0,
                    Losses = 1,
                    WinRate = 0m,
                    Ranking = 1180,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Player
                {
                    UserId = users[4].Id, // player3
                    Nickname = "MikeElite",
                    Position = PlayerPositions.Entry,
                    Country = "CA",
                    Age = 21,
                    TeamId = teams[0].Id, // Phoenix Warriors
                    // Кешовані лічильники мають відповідати рядкам MatchPlayer,
                    // що засіваються нижче для завершеного матчу Phoenix Warriors vs Dragon Slayers
                    TotalMatches = 1,
                    Wins = 1,
                    Losses = 0,
                    WinRate = 100m,
                    Ranking = 1320,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Player
                {
                    UserId = users[5].Id, // player4
                    Nickname = "EmmaChamp",
                    Position = PlayerPositions.Support,
                    Country = "DE",
                    Age = 23,
                    TeamId = teams[1].Id, // Dragon Slayers
                    // Кешовані лічильники мають відповідати рядкам MatchPlayer,
                    // що засіваються нижче для завершеного матчу Phoenix Warriors vs Dragon Slayers
                    TotalMatches = 1,
                    Wins = 0,
                    Losses = 1,
                    WinRate = 0m,
                    Ranking = 1210,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-10)
                }
            };

            await context.Players.AddRangeAsync(players);
            await context.SaveChangesAsync();

            // Создаем турниры
            var tournaments = new List<Tournament>
            {
                new Tournament
                {
                    Name = "Winter Championship 2024",
                    Description = "Annual winter tournament featuring top teams from around the world",
                    Game = Games.CS2,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    EndDate = DateTime.UtcNow.AddDays(14),
                    MaxTeams = 16,
                    CurrentTeams = 2,
                    Status = TournamentStatus.Registration,
                    PrizePool = 50000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Tournament
                {
                    Name = "Spring Showdown 2024",
                    Description = "Competitive tournament for emerging teams",
                    Game = Games.Valorant,
                    StartDate = DateTime.UtcNow.AddDays(21),
                    EndDate = DateTime.UtcNow.AddDays(28),
                    MaxTeams = 8,
                    CurrentTeams = 1,
                    Status = TournamentStatus.Registration,
                    PrizePool = 25000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Tournament
                {
                    Name = "Autumn Cup 2023",
                    Description = "Completed tournament from last season",
                    Game = Games.CS2,
                    StartDate = DateTime.UtcNow.AddDays(-60),
                    EndDate = DateTime.UtcNow.AddDays(-53),
                    MaxTeams = 12,
                    CurrentTeams = 12,
                    Status = TournamentStatus.Completed,
                    PrizePool = 35000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70),
                    UpdatedAt = DateTime.UtcNow.AddDays(-53)
                },
                // Ці два турніри існують, щоб у демо-даних були присутні всі
                // чотири дисципліни — інакше фільтр за грою нічим перевірити.
                // Додавати їх треба в кінець списку: матчі нижче посилаються
                // на tournaments[2] за індексом.
                new Tournament
                {
                    Name = "Dota Masters 2024",
                    Description = "Invitational tournament for the region's strongest rosters",
                    Game = Games.Dota2,
                    StartDate = DateTime.UtcNow.AddDays(30),
                    EndDate = DateTime.UtcNow.AddDays(37),
                    MaxTeams = 8,
                    CurrentTeams = 0,
                    Status = TournamentStatus.Registration,
                    PrizePool = 40000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Tournament
                {
                    Name = "Rift Open 2024",
                    Description = "Open qualifier ladder with a single-elimination finals bracket",
                    Game = Games.LeagueOfLegends,
                    StartDate = DateTime.UtcNow.AddDays(45),
                    EndDate = DateTime.UtcNow.AddDays(52),
                    MaxTeams = 16,
                    CurrentTeams = 0,
                    Status = TournamentStatus.Registration,
                    PrizePool = 30000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Tournaments.AddRangeAsync(tournaments);
            await context.SaveChangesAsync();

            // Создаем матчи для завершенного турнира
            var matches = new List<Match>
            {
                new Match
                {
                    TournamentId = tournaments[2].Id, // Autumn Cup 2023
                    Game = tournaments[2].Game,
                    HomeTeamId = teams[0].Id, // Phoenix Warriors
                    AwayTeamId = teams[1].Id, // Dragon Slayers
                    ScheduledAt = DateTime.UtcNow.AddDays(-55),
                    StartedAt = DateTime.UtcNow.AddDays(-55),
                    EndedAt = DateTime.UtcNow.AddDays(-55).AddHours(1),
                    Status = TournamentStatus.Completed,
                    HomeTeamScore = 16,
                    AwayTeamScore = 12,
                    WinnerTeamId = teams[0].Id, // Phoenix Warriors won
                    MatchType = MatchTypes.Final,
                    Format = "BO1",
                    Notes = "Exciting final match with great plays from both teams",
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                }
            };

            await context.Matches.AddRangeAsync(matches);
            await context.SaveChangesAsync();

            await EnsureScheduledMatchesAsync(context);

            // Создаем статистику игроков для матча
            var matchPlayers = new List<MatchPlayer>
            {
                new MatchPlayer
                {
                    MatchId = matches[0].Id,
                    PlayerId = players[0].Id, // AlexPro
                    TeamId = teams[0].Id, // Phoenix Warriors
                    Kills = 24,
                    Deaths = 15,
                    Assists = 8,
                    Champion = "AK-47",
                    IsStarter = true
                },
                new MatchPlayer
                {
                    MatchId = matches[0].Id,
                    PlayerId = players[2].Id, // MikeElite
                    TeamId = teams[0].Id, // Phoenix Warriors
                    Kills = 19,
                    Deaths = 14,
                    Assists = 12,
                    Champion = "M4A4",
                    IsStarter = true
                },
                new MatchPlayer
                {
                    MatchId = matches[0].Id,
                    PlayerId = players[1].Id, // SarahSniper
                    TeamId = teams[1].Id, // Dragon Slayers
                    Kills = 18,
                    Deaths = 16,
                    Assists = 6,
                    Champion = "AWP",
                    IsStarter = true
                },
                new MatchPlayer
                {
                    MatchId = matches[0].Id,
                    PlayerId = players[3].Id, // EmmaChamp
                    TeamId = teams[1].Id, // Dragon Slayers
                    Kills = 14,
                    Deaths = 18,
                    Assists = 15,
                    Champion = "M4A1-S",
                    IsStarter = true
                }
            };

            await context.MatchPlayers.AddRangeAsync(matchPlayers);
            await context.SaveChangesAsync();

            Console.WriteLine("База данных успешно заполнена тестовыми данными!");
        }

        private static async Task EnsureScheduledMatchesAsync(EsportsDbContext context)
        {
            if (await context.Matches.AnyAsync(match => match.Status == MatchStatus.Scheduled))
            {
                return;
            }

            var teams = await context.Teams.OrderBy(team => team.Id).ToListAsync();
            var tournaments = await context.Tournaments.OrderBy(tournament => tournament.Id).ToListAsync();

            if (teams.Count < 2 || tournaments.Count == 0)
            {
                return;
            }

            // Тримаємо самі турніри, а не лише їхні Id: матч успадковує дисципліну
            // турніру, тож без об'єкта її нема звідки взяти.
            var firstTournament = tournaments[0];
            var secondTournament = tournaments.Count > 1 ? tournaments[1] : tournaments[0];

            var scheduledMatches = new List<Match>
            {
                new Match
                {
                    TournamentId = firstTournament.Id,
                    Game = firstTournament.Game,
                    HomeTeamId = teams[0].Id,
                    AwayTeamId = teams[1].Id,
                    ScheduledAt = DateTime.UtcNow.AddDays(2).AddHours(3),
                    Status = MatchStatus.Scheduled,
                    MatchType = MatchTypes.GroupStage,
                    Format = "BO3",
                    Notes = "Upcoming group stage opener",
                    CreatedAt = DateTime.UtcNow
                },
                new Match
                {
                    TournamentId = firstTournament.Id,
                    Game = firstTournament.Game,
                    HomeTeamId = teams[1].Id,
                    AwayTeamId = teams[0].Id,
                    ScheduledAt = DateTime.UtcNow.AddDays(3).AddHours(1),
                    Status = MatchStatus.Scheduled,
                    MatchType = MatchTypes.GroupStage,
                    Format = "BO1",
                    Notes = "Second round test match",
                    CreatedAt = DateTime.UtcNow
                },
                new Match
                {
                    TournamentId = secondTournament.Id,
                    Game = secondTournament.Game,
                    HomeTeamId = teams[0].Id,
                    AwayTeamId = teams[1].Id,
                    ScheduledAt = DateTime.UtcNow.AddDays(5).AddHours(2),
                    Status = MatchStatus.Scheduled,
                    MatchType = MatchTypes.QuarterFinal,
                    Format = "BO3",
                    Notes = "Quarter-final preview",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Matches.AddRangeAsync(scheduledMatches);
            await context.SaveChangesAsync();
        }
    }
}
