namespace TForge.DTOs
{
    public class DraftStepDto
    {
        public int StepIndex { get; set; }
        public string Action { get; set; } = string.Empty;
        public int? ActingTeamId { get; set; }
        public string? ChosenValue { get; set; }
        public DateTime? ChosenAt { get; set; }
        public bool WasAutoPicked { get; set; }
    }

    public class DraftDto
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public string Game { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int FirstTurnTeamId { get; set; }
        public int CurrentStepIndex { get; set; }
        public DateTime? TurnDeadline { get; set; }
        public int TurnSeconds { get; set; }

        /// <summary>
        /// Час сервера в мить відповіді. Клієнт знімає з нього різницю зі своїм
        /// годинником і малює відлік уже за нею — інакше користувач, у якого
        /// годинник збігає на дві хвилини, бачить таймер, що суперечить серверу.
        /// </summary>
        public DateTime ServerTime { get; set; }

        public List<DraftStepDto> Steps { get; set; } = new();
    }

    public class CreateDraftDto
    {
        /// <summary>Скільки дається на хід. Порожньо — 30 секунд.</summary>
        public int? TurnSeconds { get; set; }
    }

    public class ApplyStepDto
    {
        public string Value { get; set; } = string.Empty;
    }
}
