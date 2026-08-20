using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TForge.Common;
namespace TForge.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(30)]
        public string Nickname { get; set; } = string.Empty;

        [StringLength(50)]
        public string Position { get; set; } = string.Empty; // Support, ADC, Mid, Jungle, Top тощо

        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        public int Age { get; set; } = 0;

        public int? TeamId { get; set; } // Nullable - гравець може не мати команди

        // Ігрові теги. Формати перевіряє Common/GameIdFormats.cs; null означає
        // «гравець його не вказав», а не «немає акаунта».
        [StringLength(GameIdFormats.MaxLength)]
        public string? RiotId { get; set; }

        [StringLength(GameIdFormats.MaxLength)]
        public string? SteamId64 { get; set; }

        [StringLength(GameIdFormats.MaxLength)]
        public string? BattleTag { get; set; }

        // Статистика
        public int TotalMatches { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal WinRate { get; set; } = 0.0m; // Відсоток перемог

        public int Ranking { get; set; } = 0;

        // Що ховати від інших. Показники — матчі, рейтинг, KDA — приховати не
        // можна: на них тримаються таблиці. Рішення ухвалює
        // Common/ProfileVisibility.cs.
        public bool IsAgeHidden { get; set; }

        public bool IsCountryHidden { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Team? Team { get; set; }
        public virtual ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
        public virtual ICollection<PlayerGameProfile> GameProfiles { get; set; } = new List<PlayerGameProfile>();
    }
}
