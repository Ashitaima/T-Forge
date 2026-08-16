using TForge.Common;
using TForge.Data.Interfaces;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Аватари лежать файлами на диску, а в базі зберігається лише шлях —
    /// так дампи бази не роздуваються картинками, а віддає їх статика.
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private const string RelativeFolder = "uploads/avatars";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AvatarService> _logger;

        public AvatarService(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment environment,
            ILogger<AvatarService> logger)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// WebRootPath буває null, поки теки wwwroot не існує, — тоді будуємо
        /// шлях від кореня контенту самі.
        /// </summary>
        private string WebRoot =>
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        public async Task<string> SaveAsync(int userId, IFormFile file)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

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

            var folder = Path.Combine(WebRoot, RelativeFolder);
            Directory.CreateDirectory(folder);

            var fileName = $"{userId}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var destination = File.Create(fullPath))
            await using (var source = file.OpenReadStream())
            {
                await source.CopyToAsync(destination);
            }

            var previousPath = user.AvatarPath;
            user.AvatarPath = $"/{RelativeFolder}/{fileName}";
            await _unitOfWork.SaveChangesAsync();

            // Старий файл прибираємо лише після успішного запису: якби видалили
            // раніше й збереження впало, користувач лишився б зовсім без аватара.
            DeleteFile(previousPath);

            return user.AvatarPath;
        }

        public async Task ClearAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            var previousPath = user.AvatarPath;
            user.AvatarPath = null;
            await _unitOfWork.SaveChangesAsync();

            DeleteFile(previousPath);
        }

        /// <summary>Відсутній чи заблокований файл не повинен валити запит.</summary>
        private void DeleteFile(string? relativePath)
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
                _logger.LogWarning(exception, "Не вдалося видалити файл аватара {Path}", relativePath);
            }
        }
    }
}
