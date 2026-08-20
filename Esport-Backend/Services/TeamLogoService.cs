using TForge.Common;
using TForge.Data.Interfaces;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Той самий шлях, що й у AvatarService, тільки для Team.LogoPath:
    /// робота з файлом — в ImageUploadService, тут лише команда й права.
    /// </summary>
    public class TeamLogoService : ITeamLogoService
    {
        private const string RelativeFolder = "uploads/team-logos";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageUploadService _images;

        public TeamLogoService(IUnitOfWork unitOfWork, IImageUploadService images)
        {
            _unitOfWork = unitOfWork;
            _images = images;
        }

        public async Task<string> SaveAsync(int teamId, IFormFile file, int requestingUserId, bool isAdmin)
        {
            var team = await LoadManageableAsync(teamId, requestingUserId, isAdmin);

            var previousPath = team.LogoPath;
            team.LogoPath = await _images.SaveAsync(RelativeFolder, teamId, file);
            await _unitOfWork.SaveChangesAsync();

            // Старий файл прибираємо лише після успішного запису — інакше
            // невдале збереження лишило б команду взагалі без логотипа.
            _images.DeleteFile(previousPath);

            return team.LogoPath;
        }

        public async Task ClearAsync(int teamId, int requestingUserId, bool isAdmin)
        {
            var team = await LoadManageableAsync(teamId, requestingUserId, isAdmin);

            var previousPath = team.LogoPath;
            team.LogoPath = null;
            await _unitOfWork.SaveChangesAsync();

            _images.DeleteFile(previousPath);
        }

        private async Task<Team> LoadManageableAsync(int teamId, int requestingUserId, bool isAdmin)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            if (!TeamCaptaincyPolicy.CanManage(team.CaptainId, requestingUserId, isAdmin))
            {
                throw new ForbiddenException("Змінити логотип може лише капітан команди");
            }

            return team;
        }
    }
}
