namespace TForge.Services.Interfaces
{
    /// <summary>
    /// Робота з файлами картинок — без жодного знання про користувача чи
    /// команду. Знає лише теку й id суб'єкта, тож той самий код обслуговує
    /// і аватари, і логотипи команд.
    /// </summary>
    public interface IImageUploadService
    {
        /// <summary>
        /// Перевіряє розмір і формат, пише файл і повертає шлях від кореня.
        /// Старий файл НЕ видаляє: його можна прибирати лише після того, як
        /// рядок збережено, інакше невдалий SaveChanges лишив би суб'єкта
        /// взагалі без картинки. За видалення відповідає той, хто викликав.
        /// </summary>
        Task<string> SaveAsync(string relativeFolder, int subjectId, IFormFile file);

        /// <summary>Прибирає файл. Відсутній чи заблокований не повинен валити запит.</summary>
        void DeleteFile(string? relativePath);
    }
}
