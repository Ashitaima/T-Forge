namespace TForge.DTOs
{
    /// <summary>
    /// Запит на членство для клієнта. Містить і назву команди, і нік гравця,
    /// бо той самий тип показують і на сторінці команди, і на сторінці гравця.
    /// </summary>
    public class MembershipRequestDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamTag { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string PlayerNickname { get; set; } = string.Empty;
        public string PlayerPosition { get; set; } = string.Empty;
        public int PlayerUserId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
