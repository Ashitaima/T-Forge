using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Заявки на роль організатора. Дані читає цей сервіс, а кожне рішення
    /// про право на дію ухвалює чистий OrganizerRequestPolicy.
    /// </summary>
    public class OrganizerRequestService : IOrganizerRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrganizerRequestService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrganizerRequestDto> ApplyAsync(int userId, CreateOrganizerRequestDto createDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            if (!OrganizerRequestPolicy.CanApply(user.Role))
            {
                throw new BusinessLogicException("Ця роль уже дає право проводити турніри");
            }

            var hasPending = await _unitOfWork.OrganizerRequests.ExistsAsync(r =>
                r.UserId == userId && r.Status == OrganizerRequestStatus.Pending);

            if (hasPending)
            {
                throw new BusinessLogicException("Ваша заявка вже очікує на розгляд");
            }

            var request = new OrganizerRequest
            {
                UserId = userId,
                Message = createDto.Message,
                Status = OrganizerRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.OrganizerRequests.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        public async Task<OrganizerRequestDto> ApproveAsync(int requestId, int adminUserId, bool isAdmin)
        {
            var request = await GetWithRelationsAsync(requestId);

            if (!OrganizerRequestPolicy.CanRespond(ToPolicyContext(request), isAdmin))
            {
                throw MakeRespondError(request);
            }

            // Роль і заявка мусять змінитися разом: схвалена заявка без ролі
            // означала б, що право видано лише на папері.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                request.User.Role = UserRoles.Organizer;

                request.Status = OrganizerRequestStatus.Approved;
                request.RespondedAt = DateTime.UtcNow;
                request.RespondedByUserId = adminUserId;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return await LoadDtoAsync(request.Id);
        }

        public async Task<OrganizerRequestDto> DeclineAsync(
            int requestId, int adminUserId, bool isAdmin, RespondOrganizerRequestDto respondDto)
        {
            var request = await GetWithRelationsAsync(requestId);

            if (!OrganizerRequestPolicy.CanRespond(ToPolicyContext(request), isAdmin))
            {
                throw MakeRespondError(request);
            }

            request.Status = OrganizerRequestStatus.Declined;
            request.ResponseNote = respondDto.ResponseNote;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = adminUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        public async Task<OrganizerRequestDto> CancelAsync(int requestId, int userId)
        {
            var request = await GetWithRelationsAsync(requestId);

            if (!OrganizerRequestPolicy.CanCancel(ToPolicyContext(request), userId))
            {
                if (!OrganizerRequestPolicy.IsPending(ToPolicyContext(request)))
                {
                    throw new BusinessLogicException("Заявку вже розглянуто");
                }

                throw new ForbiddenException("Відкликати заявку може лише той, хто її подав");
            }

            request.Status = OrganizerRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = userId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        public async Task<IEnumerable<OrganizerRequestDto>> GetForUserAsync(int userId)
        {
            var rows = await BaseQuery()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrganizerRequestDto>>(rows);
        }

        public async Task<IEnumerable<OrganizerRequestDto>> GetAllAsync(string? status)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(status))
            {
                if (!OrganizerRequestStatus.IsValid(status))
                {
                    throw new BusinessLogicException($"Невідомий статус заявки: {status}");
                }

                query = query.Where(r => r.Status == status);
            }

            var rows = await query
                // Ті, що чекають, — угорі: саме з ними адміністратор і працює.
                .OrderBy(r => r.Status == OrganizerRequestStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrganizerRequestDto>>(rows);
        }

        private IQueryable<OrganizerRequest> BaseQuery() =>
            _unitOfWork.OrganizerRequests.GetQueryable().Include(r => r.User);

        private async Task<OrganizerRequest> GetWithRelationsAsync(int requestId) =>
            await BaseQuery().FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new EntityNotFoundException("OrganizerRequest", requestId);

        private async Task<OrganizerRequestDto> LoadDtoAsync(int requestId) =>
            _mapper.Map<OrganizerRequestDto>(await GetWithRelationsAsync(requestId));

        private static OrganizerRequestPolicy.Context ToPolicyContext(OrganizerRequest request) =>
            new(request.Status, request.UserId);

        /// <summary>
        /// Розрізняє «заявку вже розглянуто» (400) і «ви не адміністратор» (403),
        /// бо CanRespond повертає false в обох випадках.
        /// </summary>
        private static Exception MakeRespondError(OrganizerRequest request)
        {
            if (!OrganizerRequestPolicy.IsPending(ToPolicyContext(request)))
            {
                return new BusinessLogicException("Заявку вже розглянуто");
            }

            return new ForbiddenException("Розглядати заявки може лише адміністратор");
        }
    }
}
