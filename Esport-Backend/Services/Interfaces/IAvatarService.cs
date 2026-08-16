namespace TForge.Services.Interfaces
{
    public interface IAvatarService
    {
        /// <summary>Зберігає файл і повертає шлях, записаний у користувача.</summary>
        Task<string> SaveAsync(int userId, IFormFile file);

        /// <summary>Прибирає аватар користувача разом із файлом.</summary>
        Task ClearAsync(int userId);
    }
}
