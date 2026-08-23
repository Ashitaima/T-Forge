using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Hubs;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Драфт матчу. Сервіс лише читає й пише рядки та розсилає події —
    /// рішення ухвалюють DraftSequences, DraftEngine і DraftPolicy.
    ///
    /// Ходи людини й таймера йдуть одним і тим самим шляхом (ApplyStepAsync):
    /// окремої «швидкої» гілки, яка обходить перевірку, тут немає навмисно,
    /// бо саме на ній обидва писали б у той самий крок.
    /// </summary>
    public class DraftService : IDraftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHubContext<MatchHub> _hub;

        public DraftService(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<MatchHub> hub)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hub = hub;
        }

        private IQueryable<MatchDraft> WithSteps() =>
            _unitOfWork.MatchDrafts.GetQueryable().Include(d => d.Steps);

        private async Task<MatchDraft> LoadAsync(int draftId) =>
            await WithSteps().FirstOrDefaultAsync(d => d.Id == draftId)
                ?? throw new EntityNotFoundException("Draft", draftId);

        public async Task<DraftDto?> GetForMatchAsync(int matchId)
        {
            var draft = await WithSteps().FirstOrDefaultAsync(d => d.MatchId == matchId);
            return draft == null ? null : ToDto(draft);
        }

        public async Task<DraftDto> CreateAsync(int matchId, int? turnSeconds)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(matchId)
                ?? throw new EntityNotFoundException("Match", matchId);

            // Драфт — це двоє. Поки суперника немає, немає і другого капітана.
            if (match.AwayTeamId == null)
            {
                throw new BusinessLogicException("Драфт неможливий, поки в матчі немає суперника");
            }

            if (match.Status != MatchStatus.Scheduled)
            {
                throw new BusinessLogicException("Драфт створюють до початку матчу");
            }

            if (await _unitOfWork.MatchDrafts.GetQueryable().AnyAsync(d => d.MatchId == matchId))
            {
                throw new BusinessLogicException("У цього матчу вже є драфт");
            }

            var sequence = DraftSequences.For(match.Game, match.Format);
            if (sequence.Count == 0)
            {
                throw new BusinessLogicException("Для цієї дисципліни та формату драфту немає");
            }

            // Жереб. Записуємо результат, а не спосіб: сперечатися потім немає про що.
            var firstTurnTeamId = Random.Shared.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId.Value;
            var secondTeamId = firstTurnTeamId == match.HomeTeamId ? match.AwayTeamId.Value : match.HomeTeamId;

            var draft = new MatchDraft
            {
                MatchId = matchId,
                Game = match.Game,
                Format = match.Format,
                Status = DraftStatuses.Pending,
                FirstTurnTeamId = firstTurnTeamId,
                CurrentStepIndex = 0,
                TurnSeconds = turnSeconds is > 0 and <= 300 ? turnSeconds.Value : 30
            };

            // Кроки створюємо всі одразу: виконавець кожного відомий уже тут,
            // і саме це робить драфт незмінним записом.
            for (var index = 0; index < sequence.Count; index++)
            {
                var template = sequence[index];
                draft.Steps.Add(new DraftStep
                {
                    StepIndex = index,
                    Action = template.Action,
                    ActingTeamId = template.IsFirstTurnTeam switch
                    {
                        true => firstTurnTeamId,
                        false => secondTeamId,
                        null => null
                    }
                });
            }

            await _unitOfWork.MatchDrafts.AddAsync(draft);
            await _unitOfWork.SaveChangesAsync();

            await BroadcastStatusAsync(draft);
            return ToDto(draft);
        }

        public async Task<DraftDto> StartAsync(int draftId)
        {
            var draft = await LoadAsync(draftId);

            if (draft.Status != DraftStatuses.Pending)
            {
                throw new BusinessLogicException("Драфт уже запущено");
            }

            draft.Status = DraftStatuses.InProgress;
            draft.StartedAt = DateTime.UtcNow;
            draft.CurrentStepIndex = 0;
            draft.TurnDeadline = DateTime.UtcNow.AddSeconds(draft.TurnSeconds);
            await _unitOfWork.SaveChangesAsync();

            await BroadcastStatusAsync(draft);
            return ToDto(draft);
        }

        public async Task<DraftDto> ApplyStepAsync(int draftId, int stepIndex, string value, bool wasAutoPicked)
        {
            var draft = await LoadAsync(draftId);

            var verdict = DraftEngine.CanApply(ToState(draft), stepIndex, value);
            if (!verdict.IsAllowed)
            {
                throw new BusinessLogicException(verdict.Reason!);
            }

            var step = draft.Steps.First(s => s.StepIndex == stepIndex);
            step.ChosenValue = value;
            step.ChosenAt = DateTime.UtcNow;
            step.WasAutoPicked = wasAutoPicked;

            Advance(draft);
            await _unitOfWork.SaveChangesAsync();

            await BroadcastStepAsync(draft, step);

            if (draft.Status == DraftStatuses.Completed)
            {
                await BroadcastStatusAsync(draft);
            }

            return ToDto(draft);
        }

        /// <summary>
        /// Зсуває хід далі й дорогою сама закриває Decider.
        ///
        /// Decider — єдиний крок без виконавця, тож окремого запиту на нього не
        /// існує: якби сервіс лишив його чекати, драфт спинявся б за крок до
        /// кінця, і таймер теж не мав би що з ним зробити.
        /// </summary>
        private void Advance(MatchDraft draft)
        {
            while (true)
            {
                draft.CurrentStepIndex++;

                var next = draft.Steps.FirstOrDefault(s => s.StepIndex == draft.CurrentStepIndex);
                if (next == null)
                {
                    draft.Status = DraftStatuses.Completed;
                    draft.CompletedAt = DateTime.UtcNow;
                    draft.TurnDeadline = null;
                    return;
                }

                if (next.Action != DraftActions.Decider)
                {
                    draft.TurnDeadline = DateTime.UtcNow.AddSeconds(draft.TurnSeconds);
                    return;
                }

                var remaining = DraftEngine.AvailableValues(ToState(draft), next.StepIndex);
                next.ChosenValue = remaining.FirstOrDefault();
                next.ChosenAt = DateTime.UtcNow;
                next.WasAutoPicked = false;
            }
        }

        public async Task CancelAsync(int draftId)
        {
            var draft = await LoadAsync(draftId);

            if (draft.Status == DraftStatuses.Completed)
            {
                throw new BusinessLogicException("Завершений драфт не скасовують");
            }

            draft.Status = DraftStatuses.Cancelled;
            draft.TurnDeadline = null;
            await _unitOfWork.SaveChangesAsync();

            await BroadcastStatusAsync(draft);
        }

        public async Task<(DraftPolicy.Context Context, int? ActingTeamId)> GetPolicyContextAsync(int draftId)
        {
            var draft = await LoadAsync(draftId);
            var context = await BuildContextAsync(draft.MatchId);
            var actingTeamId = draft.Steps
                .FirstOrDefault(s => s.StepIndex == draft.CurrentStepIndex)?.ActingTeamId;

            return (context, actingTeamId);
        }

        public Task<DraftPolicy.Context> GetPolicyContextForMatchAsync(int matchId) =>
            BuildContextAsync(matchId);

        private async Task<DraftPolicy.Context> BuildContextAsync(int matchId)
        {
            var row = await _unitOfWork.Matches.GetQueryable()
                .Where(m => m.Id == matchId)
                .Select(m => new
                {
                    m.TournamentId,
                    m.HomeTeamId,
                    m.AwayTeamId,
                    HomeCaptainUserId = m.HomeTeam.CaptainId,
                    AwayCaptainUserId = (int?)m.AwayTeam!.CaptainId
                })
                .FirstOrDefaultAsync()
                ?? throw new EntityNotFoundException("Match", matchId);

            return new DraftPolicy.Context(
                row.HomeTeamId,
                row.AwayTeamId,
                row.HomeCaptainUserId,
                row.AwayCaptainUserId,
                row.TournamentId);
        }

        public async Task<IReadOnlyList<int>> FindExpiredDraftIdsAsync(DateTime asOf) =>
            await _unitOfWork.MatchDrafts.GetQueryable()
                .Where(d => d.Status == DraftStatuses.InProgress
                    && d.TurnDeadline != null
                    && d.TurnDeadline <= asOf)
                .Select(d => d.Id)
                .ToListAsync();

        public async Task AutoPickAsync(int draftId)
        {
            var draft = await LoadAsync(draftId);

            if (draft.Status != DraftStatuses.InProgress)
            {
                return;
            }

            var stepIndex = draft.CurrentStepIndex;
            var available = DraftEngine.AvailableValues(ToState(draft), stepIndex);
            if (available.Count == 0)
            {
                return;
            }

            var value = available[Random.Shared.Next(available.Count)];

            // Через ту саму перевірку, що й людина: якщо капітан устиг за мить
            // до цього, CanApply відмовить, і це правильна відповідь.
            await ApplyStepAsync(draftId, stepIndex, value, wasAutoPicked: true);
        }

        private static DraftEngine.State ToState(MatchDraft draft) =>
            new(draft.Game, draft.Status, draft.CurrentStepIndex,
                draft.Steps
                    .OrderBy(s => s.StepIndex)
                    .Select(s => new DraftEngine.StepView(s.StepIndex, s.Action, s.ActingTeamId, s.ChosenValue))
                    .ToList());

        private DraftDto ToDto(MatchDraft draft)
        {
            var dto = _mapper.Map<DraftDto>(draft);
            dto.Steps = draft.Steps
                .OrderBy(s => s.StepIndex)
                .Select(_mapper.Map<DraftStepDto>)
                .ToList();
            dto.ServerTime = DateTime.UtcNow;
            return dto;
        }

        private Task BroadcastStepAsync(MatchDraft draft, DraftStep step) =>
            _hub.Clients.Group(MatchHub.GroupFor(draft.MatchId)).SendAsync(
                MatchHubEvents.DraftStepCompleted,
                new
                {
                    matchId = draft.MatchId,
                    draftId = draft.Id,
                    stepIndex = step.StepIndex,
                    action = step.Action,
                    actingTeamId = step.ActingTeamId,
                    chosenValue = step.ChosenValue,
                    wasAutoPicked = step.WasAutoPicked,
                    status = draft.Status,
                    currentStepIndex = draft.CurrentStepIndex,
                    turnDeadline = draft.TurnDeadline,
                    serverTime = DateTime.UtcNow
                });

        private Task BroadcastStatusAsync(MatchDraft draft) =>
            _hub.Clients.Group(MatchHub.GroupFor(draft.MatchId)).SendAsync(
                MatchHubEvents.DraftStatusChanged,
                new
                {
                    matchId = draft.MatchId,
                    draftId = draft.Id,
                    status = draft.Status,
                    currentStepIndex = draft.CurrentStepIndex,
                    turnDeadline = draft.TurnDeadline,
                    serverTime = DateTime.UtcNow
                });
    }
}
