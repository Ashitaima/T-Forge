using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TForge.Common;
namespace TForge.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Турнір, якому належить матч. Null — товариський матч, створений
        /// із виклику одного капітана до іншого.
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Власна назва матчу — «Вечірній скрим», «Розминка перед LAN».
        /// Має сенс для практичного матчу, який ставить капітан: турнірний
        /// впізнають за турніром і стадією. Порожня — нормальний стан.
        /// </summary>
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        public int HomeTeamId { get; set; }

        /// <summary>
        /// Суперник. Null — відкритий матч: команду-гостя ще не названо, і
        /// приєднатися може капітан будь-якої іншої команди.
        ///
        /// Показникам це не загрожує, і саме тому колонку зроблено
        /// необов'язковою, хоч дуель свого часу винесли в окрему сутність
        /// (Models/Duel.cs): дуель без команд лишається такою назавжди, а
        /// відкритий матч — лише доти, доки хтось не приєднався. Зіграти й
        /// потрапити в підсумки він за цей час не може: усі калькулятори
        /// рахують матчі зі статусом Completed, а завершити матч без
        /// суперника MatchService не дає.
        /// </summary>
        public int? AwayTeamId { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = MatchStatus.Scheduled; // див. Common/StatusConstants.cs

        public int HomeTeamScore { get; set; } = 0;
        public int AwayTeamScore { get; set; } = 0;

        public int? WinnerTeamId { get; set; } // Nullable - може бути нічия

        [StringLength(20)]
        public string MatchType { get; set; } = MatchTypes.GroupStage; // див. Common/StatusConstants.cs

        /// <summary>
        /// Дисципліна матчу. Заповнює сервер із турніру — клієнт її не задає.
        /// Для товариських матчів (без турніру) її обирає капітан.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty; // див. Common/Games.cs

        /// <summary>
        /// Раунд у турнірній сітці: 0 — матч поза сіткою, 1..n — раунди single elimination.
        /// </summary>
        public int Round { get; set; } = 0;

        [StringLength(10)]
        public string Format { get; set; } = "BO1"; // BO1, BO3, BO5 (Best of 1, 3, 5)

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>Посилання на трансляцію. Дозволені хости — див. Common/StreamUrlRules.cs.</summary>
        [StringLength(300)]
        public string? StreamUrl { get; set; }

        /// <summary>
        /// Сторінка матчу в зовнішньому трекері статистики (tracker.gg, HLTV,
        /// Dotabuff…). Заповнюється зазвичай після завершення матчу.
        /// </summary>
        [StringLength(300)]
        public string? TrackerUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Tournament? Tournament { get; set; }
        public virtual Team HomeTeam { get; set; } = null!;
        public virtual Team? AwayTeam { get; set; }
        public virtual Team? WinnerTeam { get; set; }
        public virtual ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    }
}
