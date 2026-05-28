using Computational_Practice.Data.Context;
using Computational_Practice.Models;
using Microsoft.EntityFrameworkCore;

namespace Computational_Practice.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(EsportsDbContext context)
        {
            // Убедимся, что база данных создана
            await context.Database.EnsureCreatedAsync();

            // Проверяем, есть ли уже данные
            if (await context.Users.AnyAsync())
            {
                return; // База данных уже засеяна
            }

            // Создаем пользователей
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@esports.com",
                    PasswordHash = "hashed_password_admin", // В реальности будет хешированный пароль
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "organizer1",
                    Email = "organizer1@esports.com",
                    PasswordHash = "hashed_password_organizer1",
                    FirstName = "John",
                    LastName = "Organizer",
                    Role = "Organizer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player1",
                    Email = "player1@esports.com",
                    PasswordHash = "hashed_password_player1",
                    FirstName = "Alex",
                    LastName = "Gamer",
                    Role = "Player",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player2",
                    Email = "player2@esports.com",
                    PasswordHash = "hashed_password_player2",
                    FirstName = "Sarah",
                    LastName = "Pro",
                    Role = "Player",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player3",
                    Email = "player3@esports.com",
                    PasswordHash = "hashed_password_player3",
                    FirstName = "Mike",
                    LastName = "Elite",
                    Role = "Player",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "player4",
                    Email = "player4@esports.com",
                    PasswordHash = "hashed_password_player4",
                    FirstName = "Emma",
                    LastName = "Champion",
                    Role = "Player",
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
                    Position = "Rifler",
                    Country = "Ukraine",
                    Age = 22,
                    TeamId = teams[0].Id, // Phoenix Warriors
                    TotalMatches = 145,
                    Wins = 89,
                    Losses = 56,
                    WinRate = 61.38m,
                    Ranking = 1250,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new Player
                {
                    UserId = users[3].Id, // player2
                    Nickname = "SarahSniper",
                    Position = "AWPer",
                    Country = "USA",
                    Age = 24,
                    TeamId = teams[1].Id, // Dragon Slayers
                    TotalMatches = 167,
                    Wins = 112,
                    Losses = 55,
                    WinRate = 67.07m,
                    Ranking = 1180,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-8)
                },
                new Player
                {
                    UserId = users[4].Id, // player3
                    Nickname = "MikeElite",
                    Position = "Entry Fragger",
                    Country = "Canada",
                    Age = 21,
                    TeamId = teams[0].Id, // Phoenix Warriors
                    TotalMatches = 98,
                    Wins = 62,
                    Losses = 36,
                    WinRate = 63.27m,
                    Ranking = 1320,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow.AddMonths(-4)
                },
                new Player
                {
                    UserId = users[5].Id, // player4
                    Nickname = "EmmaChamp",
                    Position = "Support",
                    Country = "Germany",
                    Age = 23,
                    TeamId = teams[1].Id, // Dragon Slayers
                    TotalMatches = 203,
                    Wins = 134,
                    Losses = 69,
                    WinRate = 65.52m,
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
                    Game = "CS2",
                    StartDate = DateTime.UtcNow.AddDays(7),
                    EndDate = DateTime.UtcNow.AddDays(14),
                    MaxTeams = 16,
                    CurrentTeams = 2,
                    Status = "Registration",
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
                    Game = "Valorant",
                    StartDate = DateTime.UtcNow.AddDays(21),
                    EndDate = DateTime.UtcNow.AddDays(28),
                    MaxTeams = 8,
                    CurrentTeams = 1,
                    Status = "Registration",
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
                    Game = "CS2",
                    StartDate = DateTime.UtcNow.AddDays(-60),
                    EndDate = DateTime.UtcNow.AddDays(-53),
                    MaxTeams = 12,
                    CurrentTeams = 12,
                    Status = "Completed",
                    PrizePool = 35000m,
                    OrganizerId = users[1].Id, // organizer1
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70),
                    UpdatedAt = DateTime.UtcNow.AddDays(-53)
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
                    HomeTeamId = teams[0].Id, // Phoenix Warriors
                    AwayTeamId = teams[1].Id, // Dragon Slayers
                    ScheduledAt = DateTime.UtcNow.AddDays(-55),
                    StartedAt = DateTime.UtcNow.AddDays(-55),
                    EndedAt = DateTime.UtcNow.AddDays(-55).AddHours(1),
                    Status = "Completed",
                    HomeTeamScore = 16,
                    AwayTeamScore = 12,
                    WinnerTeamId = teams[0].Id, // Phoenix Warriors won
                    MatchType = "Final",
                    Format = "BO1",
                    Notes = "Exciting final match with great plays from both teams",
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                }
            };

            await context.Matches.AddRangeAsync(matches);
            await context.SaveChangesAsync();

            // Создаем статистику игроков для матча
            var matchPlayers = new List<MatchPlayer>
            {
                new MatchPlayer
                {
                    MatchId = matches[0].Id,
                    PlayerId = players[0].Id, // AlexPro
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
    }
}
