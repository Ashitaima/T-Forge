using TForge.Common;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Картинки лежать файлами на диску, а в базі зберігається лише шлях —
    /// так дампи бази не роздуваються, а віддає файли статика.
    /// </summary>
    public class ImageUploadService : IImageUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageUploadService> _logger;

        public ImageUploadService(IWebHostEnvironment environment, ILogger<ImageUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// WebRootPath буває null, поки теки wwwroot не існує, — тоді будуємо
        /// шлях від кореня контенту самі.
        /// </summary>
        private string WebRoot =>
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        public async Task<string> SaveAsync(string relativeFolder, int subjectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new BusinessLogicException("Файл порожній");
            }

            if (file.Length > AvatarRules.MaxBytes)
            {
                throw new BusinessLogicException("Файл завеликий: максимум 2 МБ");
            }

            // Тип визначаємо за вмістом, а не за назвою чи Content-Type —
            // і те, і те задає клієнт.
            var head = new byte[AvatarRules.HeaderBytes];
            await using (var probe = file.OpenReadStream())
            {
                var read = await probe.ReadAsync(head);
                if (read < head.Length)
                {
                    Array.Resize(ref head, read);
                }
            }

            var extension = AvatarRules.DetectExtension(head)
                ?? throw new BusinessLogicException("Підтримуються лише JPEG, PNG і WebP");

            var folder = Path.Combine(WebRoot, relativeFolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{subjectId}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var destination = File.Create(fullPath))
            await using (var source = file.OpenReadStream())
            {
                await source.CopyToAsync(destination);
            }

            return $"/{relativeFolder}/{fileName}";
        }

        public void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            try
            {
                var fullPath = Path.Combine(WebRoot, relativePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException exception)
            {
                _logger.LogWarning(exception, "Не вдалося видалити файл {Path}", relativePath);
            }
        }
    }
}
