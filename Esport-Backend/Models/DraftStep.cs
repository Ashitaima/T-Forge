using System.ComponentModel.DataAnnotations;

namespace TForge.Models
{
    /// <summary>
    /// Один крок драфту. Рядки створюються всі одразу, з порожнім ChosenValue:
    /// екран драфту показує не лише те, що вже зроблено, а й чий хід наступний
    /// і що буде після нього.
    /// </summary>
    public class DraftStep
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DraftId { get; set; }

        /// <summary>Місце в послідовності, від нуля.</summary>
        public int StepIndex { get; set; }

        [Required]
        [StringLength(10)]
        public string Action { get; set; } = string.Empty; // див. Common/DraftActions.cs

        /// <summary>Порожньо лише в Decider: цей крок проставляє сервер.</summary>
        public int? ActingTeamId { get; set; }

        /// <summary>Порожньо, доки хід не зроблено.</summary>
        [StringLength(50)]
        public string? ChosenValue { get; set; }

        public DateTime? ChosenAt { get; set; }

        /// <summary>
        /// true — вибрав таймер, не людина. Зберігаємо окремо: бан, який зробив
        /// таймер, — не те саме, що бан, який зробив капітан, і на екрані це
        /// має бути видно.
        /// </summary>
        public bool WasAutoPicked { get; set; }

        // Navigation Properties
        public virtual MatchDraft Draft { get; set; } = null!;
    }
}
