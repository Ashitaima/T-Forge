using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Матч один на один між двома гравцями.
    ///
    /// Окрема сутність, а не Match із порожніми командами — див.
    /// docs/superpowers/specs/2026-08-19-duel-1v1-design.md. Коротко: від
    /// Match.HomeTeamId/AwayTeamId залежать усі показники проєкту (рейтинговий
    /// журнал, ростери, обидва калькулятори рекордів і підзапити в списках), і
    /// «матч без команди» довелося б окремо визначити в кожному з тих місць.
    ///
    /// Тому дуель нічого з того не чіпає: вона не створює MatchPlayer, не
    /// рухає рейтинг і не входить у Player.TotalMatches. Її рахунок рахує
    /// Common/DuelRecordCalculator.cs і показує окремим блоком.
    ///
    /// Обидві сторони — Player.Id, а не User.Id: дуель грають гравці.
    /// </summary>
    public class Duel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChallengerPlayerId { get; set; }

        /// <summary>
        /// Суперник. Null — відкритий виклик: гравця ще не названо, і
        /// прийняти його може будь-хто, крім самого ініціатора. Щойно хтось
        /// погодився, тут стоїть його Player.Id, і далі дуель нічим не
        /// відрізняється від адресної.
        /// </summary>
        public int? OpponentPlayerId { get; set; }

        /// <summary>Дисципліна. Турніру немає, тож її обирає той, хто викликає.</summary>
        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        public DateTime ScheduledAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = DuelStatuses.Pending;

        [StringLength(10)]
        public string Format { get; set; } = "BO1";

        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public int ChallengerScore { get; set; } = 0;

        public int OpponentScore { get; set; } = 0;

        /// <summary>Player.Id переможця. Null у завершеній дуелі — нічия.</summary>
        public int? WinnerPlayerId { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public virtual Player ChallengerPlayer { get; set; } = null!;
        public virtual Player? OpponentPlayer { get; set; }
    }
}
