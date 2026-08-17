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
    /// Виклики на товариські матчі. Дані читає цей сервіс, а кожне рішення
    /// про право на дію ухвалює чистий MatchChallengePolicy.
    /// </summary>
    public class MatchChallengeService : IMatchChallengeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMatchRosterService _rosterService;
        private readonly IMapper _mapper;

        public MatchChallengeService(
            IUnitOfWork unitOfWork,
            IMatchRosterService rosterService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _rosterService = rosterService;
            _mapper = mapper;
        }

        public async Task<MatchChallengeDto> CreateAsync(
            CreateMatchChallengeDto createDto, int requestingUserId, bool isAdmin)
        {
            var challenger = await _unitOfWork.Teams.GetByIdAsync(createDto.ChallengerTeamId)
                ?? throw new EntityNotFoundException("Team", createDto.ChallengerTeamId);

            var opponent = await _unitOfWork.Teams.GetByIdAsync(createDto.OpponentTeamId)
                ?? throw new EntityNotFoundException("Team", createDto.OpponentTeamId);

            // Викликати може лише капітан команди-ініціатора.
            if (!isAdmin && challenger.CaptainId != requestingUserId)
            {
                throw new ForbiddenException("Викликати на матч може лише капітан команди");
            }

            if (challenger.Id == opponent.Id)
            {
                throw new BusinessLogicException("Команда не може викликати саму себе");
            }

            if (!challenger.IsActive || !opponent.IsActive)
            {
                throw new BusinessLogicException("Обидві команди мають бути активними");
            }

            // Один відкритий виклик на пару — у будь-якому напрямі, інакше дві
            // команди могли б завалити одна одну дзеркальними дублікатами.
            // Унікальний індекс ловить лише той самий напрям, тож зустрічний
            // виклик відсікається саме тут.
            var hasPending = await _unitOfWork.MatchChallenges.ExistsAsync(c =>
                c.Status == MatchChallengeStatus.Pending
                && ((c.ChallengerTeamId == challenger.Id && c.OpponentTeamId == opponent.Id)
                    || (c.ChallengerTeamId == opponent.Id && c.OpponentTeamId == challenger.Id)));

            if (hasPending)
            {
                throw new BusinessLogicException("Виклик для цієї пари команд уже очікує на відповідь");
            }

            var challenge = new MatchChallenge
            {
                ChallengerTeamId = challenger.Id,
                OpponentTeamId = opponent.Id,
                Game = createDto.Game,
                ProposedAt = createDto.ProposedAt,
                Format = createDto.Format,
                Message = createDto.Message,
                Status = MatchChallengeStatus.Pending,
                InitiatedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MatchChallenges.AddAsync(challenge);
            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(challenge.Id);
        }

        public async Task<MatchChallengeDto> AcceptAsync(int challengeId, int requestingUserId, bool isAdmin)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var challenge = await GetWithRelationsAsync(challengeId);

                if (!MatchChallengePolicy.CanRespond(ToPolicyContext(challenge), requestingUserId, isAdmin))
                {
                    throw MakeRespondError(challenge);
                }

                // Товариський матч: без турніру і поза сіткою (Round = 0),
                // тож BracketService його не чіпає.
                var match = new Match
                {
                    TournamentId = null,
                    HomeTeamId = challenge.ChallengerTeamId,
                    AwayTeamId = challenge.OpponentTeamId,
                    ScheduledAt = challenge.ProposedAt,
                    Status = MatchStatus.Scheduled,
                    MatchType = MatchTypes.GroupStage,
                    Game = challenge.Game,
                    Format = challenge.Format,
                    Round = 0,
                    Notes = challenge.Message,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Matches.AddAsync(match);
                await _unitOfWork.SaveChangesAsync();

                challenge.Status = MatchChallengeStatus.Accepted;
                challenge.RespondedAt = DateTime.UtcNow;
                challenge.RespondedByUserId = requestingUserId;
                challenge.MatchId = match.Id;

                await _unitOfWork.SaveChangesAsync();

                // Склад проставляється одразу, як і для турнірного матчу:
                // капітани приймають виклик і бачать готовий ростер.
                await _rosterService.AutoFillAsync(match.Id);

                await _unitOfWork.CommitTransactionAsync();

                return await LoadDtoAsync(challenge.Id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MatchChallengeDto> DeclineAsync(int challengeId, int requestingUserId, bool isAdmin)
        {
            var challenge = await GetWithRelationsAsync(challengeId);

            if (!MatchChallengePolicy.CanRespond(ToPolicyContext(challenge), requestingUserId, isAdmin))
            {
                throw MakeRespondError(challenge);
            }

            challenge.Status = MatchChallengeStatus.Declined;
            challenge.RespondedAt = DateTime.UtcNow;
            challenge.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(challenge.Id);
        }

        public async Task<MatchChallengeDto> CancelAsync(int challengeId, int requestingUserId, bool isAdmin)
        {
            var challenge = await GetWithRelationsAsync(challengeId);

            if (!MatchChallengePolicy.CanCancel(ToPolicyContext(challenge), requestingUserId, isAdmin))
            {
                if (!MatchChallengePolicy.IsPending(ToPolicyContext(challenge)))
                {
                    throw new BusinessLogicException("Виклик уже закрито");
                }

                throw new ForbiddenException("Скасувати виклик може лише той, хто його надіслав");
            }

            challenge.Status = MatchChallengeStatus.Cancelled;
            challenge.RespondedAt = DateTime.UtcNow;
            challenge.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(challenge.Id);
        }

        public async Task<IEnumerable<MatchChallengeDto>> GetForTeamAsync(int teamId, string? status)
        {
            _ = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            var query = BaseQuery()
                .Where(c => c.ChallengerTeamId == teamId || c.OpponentTeamId == teamId);

            if (!string.IsNullOrEmpty(status))
            {
                if (!MatchChallengeStatus.IsValid(status))
                {
                    throw new BusinessLogicException($"Невідомий статус виклику: {status}");
                }

                query = query.Where(c => c.Status == status);
            }

            var rows = await query
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MatchChallengeDto>>(rows);
        }

        /// <summary>
        /// Виклики, що чекають саме на цього користувача — тобто ті, де він
        /// капітан викликаної команди. Живить індикатор у бічній панелі.
        /// </summary>
        public async Task<IEnumerable<MatchChallengeDto>> GetPendingForUserAsync(int userId)
        {
            var rows = await BaseQuery()
                .Where(c => c.Status == MatchChallengeStatus.Pending
                            && c.OpponentTeam.CaptainId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<MatchChallengeDto>>(rows);
        }

        private IQueryable<MatchChallenge> BaseQuery() =>
            _unitOfWork.MatchChallenges.GetQueryable()
                .Include(c => c.ChallengerTeam)
                .Include(c => c.OpponentTeam);

        private async Task<MatchChallenge> GetWithRelationsAsync(int challengeId) =>
            await BaseQuery().FirstOrDefaultAsync(c => c.Id == challengeId)
                ?? throw new EntityNotFoundException("MatchChallenge", challengeId);

        private async Task<MatchChallengeDto> LoadDtoAsync(int challengeId) =>
            _mapper.Map<MatchChallengeDto>(await GetWithRelationsAsync(challengeId));

        private static MatchChallengePolicy.Context ToPolicyContext(MatchChallenge challenge) =>
            new(challenge.Status,
                challenge.InitiatedByUserId,
                challenge.ChallengerTeam.CaptainId,
                challenge.OpponentTeam.CaptainId);

        /// <summary>
        /// Розрізняє «виклик уже закрито» (400) і «ви не та сторона» (403),
        /// бо CanRespond повертає false в обох випадках.
        /// </summary>
        private static Exception MakeRespondError(MatchChallenge challenge)
        {
            if (!MatchChallengePolicy.IsPending(ToPolicyContext(challenge)))
            {
                return new BusinessLogicException("Виклик уже закрито");
            }

            return new ForbiddenException("Відповісти на виклик може лише капітан викликаної команди");
        }
    }
}
