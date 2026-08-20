namespace TForge.DTOs
{
    public class PlayerDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public int Ranking { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }

        // Ігрові теги гравця — див. Common/GameIdFormats.cs
        public string? RiotId { get; set; }
        public string? SteamId64 { get; set; }
        public string? BattleTag { get; set; }

        /// <summary>Готове посилання на профіль Steam, або null. Будує сервер,
        /// щоб клієнт не склеював URL сам.</summary>
        public string? SteamProfileUrl { get; set; }

        public UserDto? User { get; set; }
        public TeamSummaryDto? Team { get; set; }
    }

    public class PlayerSummaryDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Акаунт гравця. Потрібен клієнту, щоб зіставити склад із
        /// Team.CaptainId — інакше в списку не видно, хто саме капітан.
        /// Той самий привід, що у MatchDto.HomeTeamCaptainId.
        /// </summary>
        public int UserId { get; set; }

        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Рядок списку гравців. Показники виведені з рядків MatchPlayer, а не з
    /// кешованих лічильників у players — так список збігається з профілем
    /// гравця навіть тоді, коли кеш розійшовся.
    /// </summary>
    public class PlayerRowDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? TeamTag { get; set; }
        public string? TeamLogoPath { get; set; }
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public double Kda { get; set; }

        /// <summary>
        /// Найвищий рейтинг гравця серед дисциплін, або null — гравець без
        /// турнірних матчів не має рейтингу взагалі й показується як «без
        /// рейтингу», а не як фальшива тисяча.
        /// </summary>
        public int? Rating { get; set; }

        /// <summary>Дисципліна, до якої належить показаний рейтинг.</summary>
        public string? RatingGame { get; set; }

        public string? RatingTier { get; set; }
    }

    public class CreatePlayerDto
    {
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? RiotId { get; set; }
        public string? SteamId64 { get; set; }
        public string? BattleTag { get; set; }
        /// <summary>Заповнює сервер з токена. Задати вручну може лише адміністратор.</summary>
        public int? UserId { get; set; }
    }

    /// <summary>
    /// Адміністратор створює обліковий запис і профіль гравця одним запитом.
    /// Роль завжди Player — інші ролі створюються через /api/users.
    /// </summary>
    public class CreateFullPlayerDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class UpdatePlayerDto
    {
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? RiotId { get; set; }
        public string? SteamId64 { get; set; }
        public string? BattleTag { get; set; }
    }

    public class PlayerStatsDto
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public double AverageKills { get; set; }
        public double AverageDeaths { get; set; }
        public double AverageAssists { get; set; }
        public double KDRatio { get; set; }
    }

    /// <summary>
    /// Профіль гравця. Усі показники нижче виведені з рядків MatchPlayer,
    /// тож вони узгоджені з таблицею гравців і журналом матчів.
    /// </summary>
    public class PlayerProfileDto
    {
        public PlayerDto? Player { get; set; }
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public double Kda { get; set; }

        /// <summary>Рейтинг за кожною дисципліною, у якій гравець грав турнірні матчі.</summary>
        public List<RatingDto> Ratings { get; set; } = new();
    }
}
