namespace TForge.DTOs
{
    /// <summary>
    /// Запит на участь команди в турнірі. Direction розрізняє лише те,
    /// хто відповідає: на Invite — капітан, на Application — організатор.
    /// </summary>
    public class TournamentInvitationDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string TournamentGame { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamTag { get; set; } = string.Empty;

        /// <summary>Потрібен клієнту, щоб показати дії саме тій стороні.</summary>
        public int TeamCaptainId { get; set; }
        public int OrganizerId { get; set; }

        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class CreateTournamentInvitationDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
