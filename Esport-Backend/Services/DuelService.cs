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
    /// Дуелі 1 на 1. Сервіс лише читає й пише рядки — правила ухвалюють
    /// DuelPolicy, DuelStatuses та DuelRecordCalculator.
    ///
    /// Тут навмисно немає нічого про рейтинг, ростери й лічильники гравця:
    /// дуель їх не чіпає, і саме заради цього вона окрема сутність.
    /// </summary>
    public class DuelService : IDuelService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DuelService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private IQueryable<Duel> WithPlayers() =>
            _unitOfWork.Duels.GetQueryable()
                .Include(d => d.ChallengerPlayer)
                .Include(d => d.OpponentPlayer);

        public async Task<IEnumerable<DuelDto>> GetAllAsync(int? playerId = null)
        {
            var query = WithPlayers();

            if (playerId.HasValue)
            {
                query = query.Where(d =>
                    d.ChallengerPlayerId == playerId || d.OpponentPlayerId == playerId);
            }

            var duels = await query
                .OrderByDescending(d => d.ScheduledAt)
                .ToListAsync();

            return duels.Select(_mapper.Map<DuelDto>);
        }

        public async Task<DuelDto?> GetByIdAsync(int id)
        {
            var duel = await WithPlayers().FirstOrDefaultAsync(d => d.Id == id);
            return duel == null ? null : _mapper.Map<DuelDto>(duel);
        }

        public async Task<DuelRecordDto> GetRecordAsync(int playerId)
        {
            // Рахуємо в пам'яті тим самим калькулятором, що й тести: правило
            // одне, і другої його копії в SQL бути не повинно.
            var duels = await _unitOfWork.Duels.GetQueryable()
                .Where(d => d.ChallengerPlayerId == playerId || d.OpponentPlayerId == playerId)
                .ToListAsync();

            var record = DuelRecordCalculator.Calculate(duels, playerId);

            return new DuelRecordDto
            {
                Played = record.Played,
                Wins = record.Wins,
                Losses = record.Losses,
                Draws = record.Draws,
                WinRate = record.WinRate
            };
        }

        public async Task<DuelPolicy.Context> GetPolicyContextAsync(int id)
        {
            var context = await _unitOfWork.Duels.GetQueryable()
                .Where(d => d.Id == id)
                .Select(d => new DuelPolicy.Context(
                    d.Status,
                    d.ChallengerPlayer.UserId,
                    d.OpponentPlayer == null ? (int?)null : d.OpponentPlayer.UserId))
                .FirstOrDefaultAsync();

            return context ?? throw new EntityNotFoundException("Duel", id);
        }

        public async Task<DuelDto> CreateAsync(CreateDuelDto createDto, int requestingUserId)
        {
            if (!Games.IsValid(createDto.Game))
            {
                throw new BusinessLogicException("Оберіть дисципліну дуелі");
            }

            var challenger = await _unitOfWork.Players.GetQueryable()
                .FirstOrDefaultAsync(p => p.UserId == requestingUserId)
                ?? throw new BusinessLogicException(
                    "Викликати на дуель може лише гравець із профілем");

            // Суперника може й не бути: відкритий виклик приймає будь-хто.
            Player? opponent = null;

            if (createDto.OpponentPlayerId is int opponentId)
            {
                opponent = await _unitOfWork.Players.GetByIdAsync(opponentId)
                    ?? throw new EntityNotFoundException("Player", opponentId);

                if (challenger.Id == opponent.Id)
                {
                    throw new BusinessLogicException("Не можна викликати самого себе");
                }

                // Один адресний виклик у цьому напрямі тримає індекс; зустрічний
                // (суперник викликає у відповідь) індекс не бачить — пара колонок
                // там у зворотному порядку, тож відсіюємо його тут.
                var alreadyOpen = await _unitOfWork.Duels.GetQueryable()
                    .AnyAsync(d => d.Status == DuelStatuses.Pending
                                   && ((d.ChallengerPlayerId == challenger.Id
                                        && d.OpponentPlayerId == opponent.Id)
                                       || (d.ChallengerPlayerId == opponent.Id
                                           && d.OpponentPlayerId == challenger.Id)));

                if (alreadyOpen)
                {
                    throw new BusinessLogicException("Виклик між цими гравцями вже очікує відповіді");
                }
            }

            var duel = new Duel
            {
                ChallengerPlayerId = challenger.Id,
                OpponentPlayerId = opponent?.Id,
                Game = createDto.Game,
                ScheduledAt = createDto.ScheduledAt,
                Format = createDto.Format,
                Message = createDto.Message ?? string.Empty,
                Status = DuelStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Duels.AddAsync(duel);
            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(duel.Id);
        }

        public async Task<DuelDto> RespondAsync(int id, bool accept, int requestingUserId)
        {
            var duel = await GetOrThrowAsync(id);

            // Прийняти відкритий виклик — це і є назватися суперником. Доти
            // OpponentPlayerId порожній, і саме тут він уперше отримує значення.
            if (duel.OpponentPlayerId == null)
            {
                if (!accept)
                {
                    // Відхилити відкритий виклик нема кому: він адресований
                    // усім і водночас нікому. Просто нічого не робимо.
                    throw new BusinessLogicException(
                        "Відкритий виклик не відхиляють — його або приймають, або лишають");
                }

                var responder = await _unitOfWork.Players.GetQueryable()
                    .FirstOrDefaultAsync(p => p.UserId == requestingUserId)
                    ?? throw new BusinessLogicException(
                        "Прийняти виклик може лише гравець із профілем");

                if (responder.Id == duel.ChallengerPlayerId)
                {
                    throw new BusinessLogicException("Не можна прийняти власний виклик");
                }

                duel.OpponentPlayerId = responder.Id;
            }

            duel.Status = accept ? DuelStatuses.Accepted : DuelStatuses.Declined;
            duel.RespondedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return await LoadDtoAsync(id);
        }

        public async Task<DuelDto> CancelAsync(int id)
        {
            var duel = await GetOrThrowAsync(id);

            duel.Status = DuelStatuses.Cancelled;
            duel.RespondedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return await LoadDtoAsync(id);
        }

        public async Task<DuelDto> StartAsync(int id)
        {
            var duel = await GetOrThrowAsync(id);

            duel.Status = DuelStatuses.InProgress;
            duel.StartedAt ??= DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return await LoadDtoAsync(id);
        }

        public async Task<DuelDto> CompleteAsync(int id, CompleteDuelDto completeDto)
        {
            var duel = await GetOrThrowAsync(id);

            // Переможець мусить бути одним із двох учасників. Null — нічия,
            // і це нормальний результат, а не пропущене поле.
            if (completeDto.WinnerPlayerId is int winner
                && winner != duel.ChallengerPlayerId
                && winner != duel.OpponentPlayerId)
            {
                throw new BusinessLogicException("Переможцем може бути лише учасник дуелі");
            }

            duel.ChallengerScore = completeDto.ChallengerScore;
            duel.OpponentScore = completeDto.OpponentScore;
            duel.WinnerPlayerId = completeDto.WinnerPlayerId;
            duel.Status = DuelStatuses.Completed;
            duel.EndedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return await LoadDtoAsync(id);
        }

        private async Task<Duel> GetOrThrowAsync(int id) =>
            await _unitOfWork.Duels.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("Duel", id);

        private async Task<DuelDto> LoadDtoAsync(int id) =>
            await GetByIdAsync(id) ?? throw new EntityNotFoundException("Duel", id);
    }
}
