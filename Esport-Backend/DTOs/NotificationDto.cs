namespace TForge.DTOs
{
    /// <summary>
    /// Одне сповіщення. Власної таблиці сповіщення не мають — це проєкція
    /// рядка запиту, який і так існує. Тож скасований виклик перестає бути
    /// сповіщенням тієї ж миті, коли його скасували: застаріти тут нічому.
    /// </summary>
    public class NotificationDto
    {
        /// <summary>Ключ із Common/NotificationKinds.cs. Підпис — на фронтенді.</summary>
        public string Kind { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        /// <summary>Повідомлення з самого запиту, якщо воно було.</summary>
        public string? Body { get; set; }

        /// <summary>Маршрут клієнта, де на це можна відповісти.</summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>RespondedAt, якщо відповідь є, інакше CreatedAt.</summary>
        public DateTime CreatedAt { get; set; }

        public bool IsUnread { get; set; }

        /// <summary>Рядок відкритий і чекає саме на цього користувача.</summary>
        public bool IsActionable { get; set; }
    }
}
