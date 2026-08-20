namespace TForge.DTOs
{
    /// <summary>
    /// Заявка на роль організатора для клієнта. Містить логін заявника, бо
    /// той самий тип показують і в списку адміністратора, і самому заявнику.
    /// </summary>
    public class OrganizerRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResponseNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>Заявника визначає сервер за токеном — як і скрізь, де є власник.</summary>
    public class CreateOrganizerRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Відмова пояснюється; схвалення — ні.</summary>
    public class RespondOrganizerRequestDto
    {
        public string ResponseNote { get; set; } = string.Empty;
    }
}
