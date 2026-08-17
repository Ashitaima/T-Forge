using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;
using TForge.Exceptions;

namespace TForge.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TournamentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TournamentDto>> GetAllActiveAsync()
        {
            var tournaments = await _unitOfWork.Tournaments.GetActiveAsync();
            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<TournamentDto?> GetByIdAsync(int id)
        {
            var tournament = await _unitOfWork.Tournaments.GetByIdAsync(id);
            return tournament != null ? _mapper.Map<TournamentDto>(tournament) : null;
        }

        public async Task<TournamentDto?> GetWithMatchesAsync(int id)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithMatchesAsync(id);
            return tournament != null ? _mapper.Map<TournamentDto>(tournament) : null;
        }

        public async Task<IEnumerable<TournamentDto>> GetByStatusAsync(string status)
        {
            var tournaments = await _unitOfWork.Tournaments.GetByStatusAsync(status);
            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<IEnumerable<TournamentDto>> GetByOrganizerAsync(int organizerId)
        {
            var tournaments = await _unitOfWork.Tournaments.GetByOrganizerAsync(organizerId);
            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<IEnumerable<TournamentDto>> GetByGameAsync(string game)
        {
            var tournaments = await _unitOfWork.Tournaments.GetByGameAsync(game);
            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<IEnumerable<TournamentDto>> GetUpcomingAsync()
        {
            var tournaments = await _unitOfWork.Tournaments.GetUpcomingAsync();
            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<TournamentDto> CreateAsync(CreateTournamentDto createDto, int organizerId)
        {
            var tournament = _mapper.Map<Tournament>(createDto);
            tournament.OrganizerId = organizerId;
            await _unitOfWork.Tournaments.AddAsync(tournament);
            await _unitOfWork.SaveChangesAsync();

            // GetByIdAsync (FindAsync) не підвантажує Organizer — беремо версію з Include
            var createdTournament = await _unitOfWork.Tournaments.GetWithMatchesAsync(tournament.Id);
            return _mapper.Map<TournamentDto>(createdTournament);
        }

        public async Task<TournamentDto?> UpdateAsync(int id, UpdateTournamentDto updateDto)
        {
            var tournament = await _unitOfWork.Tournaments.GetByIdAsync(id);
            if (tournament == null)
                throw new EntityNotFoundException("Tournament", id);

            _mapper.Map(updateDto, tournament);
            _unitOfWork.Tournaments.Update(tournament);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TournamentDto>(tournament);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tournament = await _unitOfWork.Tournaments.GetByIdAsync(id);
            if (tournament == null)
                throw new EntityNotFoundException("Tournament", id);

            tournament.IsActive = false;
            _unitOfWork.Tournaments.Update(tournament);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TeamSummaryDto>> GetRegisteredTeamsAsync(int tournamentId)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            return _mapper.Map<IEnumerable<TeamSummaryDto>>(tournament.Teams.OrderBy(t => t.Id));
        }

        public async Task RegisterTeamAsync(int tournamentId, int teamId, int requestingUserId, bool isAdmin)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            // Рішення ухвалює чиста політика: на відкритому турнірі команду
            // реєструє її капітан, на закритому — лише організатор.
            if (!TournamentInvitationPolicy.CanRegisterDirectly(
                    tournament.IsInviteOnly, requestingUserId, tournament.OrganizerId, team.CaptainId, isAdmin))
            {
                throw new ForbiddenException(tournament.IsInviteOnly
                    ? "Турнір закритий: потрібне запрошення організатора або схвалена заявка"
                    : "Зареєструвати команду може лише її капітан або організатор турніру");
            }

            await AdmitAsync(tournament, team);
        }

        public async Task AdmitTeamAsync(int tournamentId, int teamId)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            await AdmitAsync(tournament, team);
        }

        /// <summary>Стан турніру та складу — те, що не залежить від того, хто просить.</summary>
        private async Task AdmitAsync(Tournament tournament, Team team)
        {
            if (!tournament.IsActive)
            {
                throw new BusinessLogicException("Турнір неактивний");
            }

            if (tournament.Status != TournamentStatus.Registration)
            {
                throw new BusinessLogicException("Реєстрацію на цей турнір закрито");
            }

            if (!team.IsActive)
            {
                throw new BusinessLogicException("Команда неактивна");
            }

            if (tournament.Teams.Any(t => t.Id == team.Id))
            {
                throw new BusinessLogicException("Команду вже зареєстровано на цей турнір");
            }

            if (tournament.Teams.Count >= tournament.MaxTeams)
            {
                throw new BusinessLogicException(
                    $"Досягнуто ліміт команд для цього турніру ({tournament.MaxTeams})");
            }

            tournament.Teams.Add(team);
            tournament.CurrentTeams = tournament.Teams.Count;
            tournament.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task WithdrawTeamAsync(int tournamentId, int teamId, int requestingUserId, bool isAdmin)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            var team = tournament.Teams.FirstOrDefault(t => t.Id == teamId)
                ?? throw new BusinessLogicException("Команду не зареєстровано на цей турнір");

            if (!isAdmin && team.CaptainId != requestingUserId && tournament.OrganizerId != requestingUserId)
            {
                throw new ForbiddenException(
                    "Зняти команду може лише її капітан або організатор турніру");
            }

            if (tournament.Status != TournamentStatus.Registration)
            {
                throw new BusinessLogicException(
                    "Знятися можна лише поки триває реєстрація");
            }

            tournament.Teams.Remove(team);
            tournament.CurrentTeams = tournament.Teams.Count;
            tournament.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<TournamentStatsDto> GetStatsAsync()
        {
            var totalActive = await _unitOfWork.Tournaments.CountAsync(t => t.IsActive);
            var activeCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == TournamentStatus.InProgress && t.IsActive);
            var completedCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == TournamentStatus.Completed && t.IsActive);
            var registrationCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == TournamentStatus.Registration && t.IsActive);

            var allTournaments = await _unitOfWork.Tournaments.FindAsync(t => t.IsActive);
            var totalPrizePool = allTournaments.Where(t => t.PrizePool > 0).Sum(t => t.PrizePool);

            var popularGames = allTournaments
                .GroupBy(t => t.Game)
                .Select(g => new GameStatsDto { Game = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return new TournamentStatsDto
            {
                TotalTournaments = totalActive,
                ActiveTournaments = activeCount,
                CompletedTournaments = completedCount,
                RegistrationOpen = registrationCount,
                TotalPrizePool = totalPrizePool,
                PopularGames = popularGames
            };
        }

        public async Task<PagedResponse<TournamentDto>> GetPagedAsync(TournamentFilter filter)
        {
            // GetQueryable, а не GetAllAsync().AsQueryable(): другий варіант витягує
            // всю таблицю в памʼять і робить запит LINQ-to-Objects, у якому
            // ApplyPaging падає на EF.Property («may only be used within EF LINQ
            // queries»). Через це ендпоінт віддавав 400 на кожен виклик.
            IQueryable<Tournament> query = _unitOfWork.Tournaments.GetQueryable()
                .Include(t => t.Organizer);

            // Застосовуємо фільтри
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(t => t.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Game))
                query = query.Where(t => t.Game.Contains(filter.Game));

            if (filter.StartDateFrom.HasValue)
                query = query.Where(t => t.StartDate >= filter.StartDateFrom.Value);

            if (filter.StartDateTo.HasValue)
                query = query.Where(t => t.StartDate <= filter.StartDateTo.Value);

            if (filter.MinPrizePool.HasValue)
                query = query.Where(t => t.PrizePool >= filter.MinPrizePool.Value);

            if (filter.MaxPrizePool.HasValue)
                query = query.Where(t => t.PrizePool <= filter.MaxPrizePool.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            if (filter.OrganizerId.HasValue)
                query = query.Where(t => t.OrganizerId == filter.OrganizerId.Value);

            // Застосовуємо пошук
            query = query.ApplySearch(filter.Search, "Name", "Description", "Game");

            // Застосовуємо сортування
            query = query.ApplySorting(filter.SortBy, filter.SortDirection);

            // Отримуємо загальну кількість
            var totalCount = await query.CountAsync();

            // Застосовуємо пагінацію
            var data = await query.ApplyPaging(filter).ToListAsync();

            var tournamentDtos = _mapper.Map<IEnumerable<TournamentDto>>(data);

            return new PagedResponse<TournamentDto>
            {
                Data = tournamentDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
}
