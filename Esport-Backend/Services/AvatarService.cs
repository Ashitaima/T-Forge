using TForge.Data.Interfaces;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Аватар користувача. Уся робота з файлом — в ImageUploadService;
    /// тут лишається тільки те, що стосується саме користувача.
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private const string RelativeFolder = "uploads/avatars";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageUploadService _images;

        public AvatarService(IUnitOfWork unitOfWork, IImageUploadService images)
        {
            _unitOfWork = unitOfWork;
            _images = images;
        }

        public async Task<string> SaveAsync(int userId, IFormFile file)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            var previousPath = user.AvatarPath;
            user.AvatarPath = await _images.SaveAsync(RelativeFolder, userId, file);
            await _unitOfWork.SaveChangesAsync();

            // Старий файл прибираємо лише після успішного запису: якби видалили
            // раніше й збереження впало, користувач лишився б зовсім без аватара.
            _images.DeleteFile(previousPath);

            return user.AvatarPath;
        }

        public async Task ClearAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            var previousPath = user.AvatarPath;
            user.AvatarPath = null;
            await _unitOfWork.SaveChangesAsync();

            _images.DeleteFile(previousPath);
        }
    }
}
