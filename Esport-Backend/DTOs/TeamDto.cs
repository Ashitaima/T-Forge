namespace TForge.DTOs
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto? Captain { get; set; }
        public List<PlayerSummaryDto> Players { get; set; } = new();
    }

    public class TeamSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public UserDto? Captain { get; set; }
    }

    /// <summary>
    /// Рядок списку команд. Показники рахуються з матчів команди — це ті самі
    /// числа, що раніше показувала окрема сторінка таблиці.
    /// Титул = перемога в матчі типу Final, тож товариські матчі його не дають.
    /// </summary>
    public class TeamRowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Потрібен списку, щоб показати кнопки редагування лише капітанові.</summary>
        public int CaptainId { get; set; }
        public string? CaptainUsername { get; set; }

        public int PlayerCount { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public int Titles { get; set; }
    }

    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;

        /// <summary>Заповнює сервер з токена. Задати вручну може лише адміністратор.</summary>
        public int? CaptainId { get; set; }
    }

    public class UpdateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
