using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Extensions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Вакансії у складі команди. Дисципліну обирає капітан — на відміну від
    /// Match.Game, тут її нема звідки успадкувати: турніру немає.
    /// </summary>
    public class TeamOpeningService : ITeamOpeningService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TeamOpeningService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TeamOpeningDto> CreateAsync(CreateTeamOpeningDto dto, int userId, bool isAdmin)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(dto.TeamId)
                ?? throw new EntityNotFoundException("Team", dto.TeamId);

            if (!LftPolicy.CanManageOpening(team.CaptainId, userId, isAdmin))
            {
                throw new BusinessLogicException("Публікувати вакансії може лише капітан команди");
            }

            // Дві відкриті вакансії на ту саму гру й позицію означали б дві
            // стрічки з тими самими картками — і два змахи там, де людина
            // погодилась один раз.
            var duplicate = await _unitOfWork.TeamOpenings.GetQueryable()
                .AnyAsync(o => o.TeamId == dto.TeamId
                            && o.Game == dto.Game
                            && o.Position == dto.Position
                            && o.Status == TeamOpeningStatus.Open);

            if (duplicate)
            {
                throw new BusinessLogicException("Така вакансія вже відкрита");
            }

            var opening = _mapper.Map<TeamOpening>(dto);
            opening.Status = TeamOpeningStatus.Open;
            opening.CreatedByUserId = userId;

            await _unitOfWork.TeamOpenings.AddAsync(opening);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(opening.Id);
        }

        public async Task<TeamOpeningDto> UpdateAsync(int id, UpdateTeamOpeningDto dto, int userId, bool isAdmin)
        {
            var opening = await LoadAsync(id);

            if (!LftPolicy.CanManageOpening(opening.Team.CaptainId, userId, isAdmin))
            {
                throw new BusinessLogicException("Змінити вакансію може лише капітан команди");
            }

            if (opening.Status != TeamOpeningStatus.Open)
            {
                throw new BusinessLogicException("Закриту вакансію змінити не можна");
            }

            // UpdateTeamOpeningDto навмисно не несе Game (дисципліну на живій
            // вакансії не міняють), тож Position і MinRank звіряємо з нею тут,
            // де opening.Game вже завантажено — валідатор цієї пари побачити
            // не може. Те саме правило, що CreateTeamOpeningValidator звіряє
            // проти dto.Game.
            if (!GamePositions.IsValidFor(opening.Game, dto.Position))
            {
                throw new BusinessLogicException("Ця позиція не належить обраній дисципліні");
            }

            if (!GameRanks.IsValidFor(opening.Game, dto.MinRank))
            {
                throw new BusinessLogicException("Цей ранг не належить обраній дисципліні");
            }

            // Той самий запобіжник, що й у CreateAsync: капітан міг
            // відредагувати Position так, що вона зіткнулася з іншою
            // відкритою вакансією тієї самої команди й гри. Власний рядок
            // виключаємо — інакше вакансія конфліктувала б сама з собою.
            var duplicate = await _unitOfWork.TeamOpenings.GetQueryable()
                .AnyAsync(o => o.Id != id
                            && o.TeamId == opening.TeamId
                            && o.Game == opening.Game
                            && o.Position == dto.Position
                            && o.Status == TeamOpeningStatus.Open);

            if (duplicate)
            {
                throw new BusinessLogicException("Така вакансія вже відкрита");
            }

            _mapper.Map(dto, opening);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<TeamOpeningDto> CloseAsync(int id, int userId, bool isAdmin)
        {
            var opening = await LoadAsync(id);

            if (!LftPolicy.CanManageOpening(opening.Team.CaptainId, userId, isAdmin))
            {
                throw new BusinessLogicException("Закрити вакансію може лише капітан команди");
            }

            if (opening.Status == TeamOpeningStatus.Open)
            {
                opening.Status = TeamOpeningStatus.Cancelled;
                opening.ClosedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }

            return await GetByIdAsync(id);
        }

        public async Task<TeamOpeningDto> GetByIdAsync(int id) =>
            _mapper.Map<TeamOpeningDto>(await LoadAsync(id));

        public async Task<PagedResponse<TeamOpeningDto>> GetPagedAsync(TeamOpeningFilter filter)
        {
            // Явно типізуємо як IQueryable<TeamOpening>: без цього var
            // застигає на IIncludableQueryable з Include, і подальші
            // query = query.Where(...) нижче не компілюються.
            IQueryable<TeamOpening> query = _unitOfWork.TeamOpenings.GetQueryable()
                .Include(o => o.Team);

            if (!string.IsNullOrEmpty(filter.Game))
            {
                query = query.Where(o => o.Game == filter.Game);
            }

            if (!string.IsNullOrEmpty(filter.Position))
            {
                query = query.Where(o => o.Position == filter.Position);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(o => o.Status == filter.Status);
            }

            if (filter.TeamId.HasValue)
            {
                query = query.Where(o => o.TeamId == filter.TeamId.Value);
            }

            // Порядок явний: ApplyPaging інакше впаде на Id, і сторінки
            // перестануть відповідати «найновіші спершу».
            query = query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id);

            return await query.ToPagedResponseAsync<TeamOpening, TeamOpeningDto>(filter, _mapper);
        }

        public async Task<IEnumerable<TeamOpeningDto>> GetByTeamAsync(int teamId)
        {
            var openings = await _unitOfWork.TeamOpenings.GetQueryable()
                .Include(o => o.Team)
                .Where(o => o.TeamId == teamId)
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeamOpeningDto>>(openings);
        }

        private async Task<TeamOpening> LoadAsync(int id) =>
            await _unitOfWork.TeamOpenings.GetQueryable()
                .Include(o => o.Team)
                .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new EntityNotFoundException("TeamOpening", id);
    }
}
