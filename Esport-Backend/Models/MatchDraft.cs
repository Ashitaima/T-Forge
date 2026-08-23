using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Драфт матчу: вето карт у CS2 і Valorant, підбір героїв у Dota 2 і LoL.
    ///
    /// Один рядок на матч. Кроки створюються всі одразу (Models/DraftStep.cs) —
    /// так драфт лишається незмінним записом навіть після того, як
    /// Common/DraftSequences.cs перебудують під новий патч.
    ///
    /// Матчу драфт не заважає: матч як починався, так і починається, драфту в
    /// нього може не бути взагалі.
    /// </summary>
    public class MatchDraft
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MatchId { get; set; }

        /// <summary>
        /// Копія з матчу — тим самим правилом, що й Match.Game з турніру.
        /// Послідовність уже породжено, і зміна дисципліни заднім числом не
        /// сміє перетворити двадцять рядків на вісім.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Format { get; set; } = "BO1";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = DraftStatuses.Pending;

        /// <summary>
        /// Переможець жеребкування: ця команда ходить першою й отримує перший
        /// пік. Записано, а не обчислено, — щоб потім не було про що сперечатися.
        /// </summary>
        [Required]
        public int FirstTurnTeamId { get; set; }

        /// <summary>Крок, якого чекаємо. Дорівнює кількості кроків — драфт скінчено.</summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>Коли згорить поточний хід. Порожньо — драфт не триває.</summary>
        public DateTime? TurnDeadline { get; set; }

        public int TurnSeconds { get; set; } = 30;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        public virtual Match Match { get; set; } = null!;
        public virtual ICollection<DraftStep> Steps { get; set; } = new List<DraftStep>();
    }
}
