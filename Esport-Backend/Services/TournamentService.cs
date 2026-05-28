using AutoMapper;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.DTOs;
using Computational_Practice.Models;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Extensions;
using Computational_Practice.Exceptions;

namespace Computational_Practice.Services
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

        public async Task<TournamentDto> CreateAsync(CreateTournamentDto createDto)
        {
            var tournament = _mapper.Map<Tournament>(createDto);
            await _unitOfWork.Tournaments.AddAsync(tournament);
            await _unitOfWork.SaveChangesAsync();

            var createdTournament = await _unitOfWork.Tournaments.GetByIdAsync(tournament.Id);
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

        public async Task<TournamentStatsDto> GetStatsAsync()
        {
            var totalActive = await _unitOfWork.Tournaments.CountAsync(t => t.IsActive);
            var activeCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == "Active" && t.IsActive);
            var completedCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == "Completed" && t.IsActive);
            var registrationCount = await _unitOfWork.Tournaments.CountAsync(t => t.Status == "Registration" && t.IsActive);

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
            var query = (await _unitOfWork.Tournaments.GetAllAsync()).AsQueryable();

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
            var totalCount = query.Count();

            // Застосовуємо пагінацію
            var data = query.ApplyPaging(filter).ToList();

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
