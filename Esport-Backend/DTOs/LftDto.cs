namespace TForge.DTOs
{
    public class SwipeDto
    {
        public int TeamOpeningId { get; set; }

        /// <summary>
        /// Кандидат. Заповнює лише капітан, гортаючи гравців; коли змахує сам
        /// гравець, сервер бере його з токена й це поле ігнорує.
        /// </summary>
        public int? PlayerId { get; set; }

        /// <summary>true — «подобається», false — «пропустити».</summary>
        public bool Like { get; set; }
    }

    public class SwipeResultDto
    {
        /// <summary>Чи склалася взаємна згода і гравець потрапив у склад.</summary>
        public bool Matched { get; set; }

        /// <summary>Id створеного або прийнятого запиту; null для «пропустити».</summary>
        public int? MembershipRequestId { get; set; }
    }
}
