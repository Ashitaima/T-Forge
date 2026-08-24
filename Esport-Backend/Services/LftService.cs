using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Extensions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Пошук складу: гравець гортає вакансії, капітан — кандидатів.
    ///
    /// Взаємна згода не створює нічого нового — вона приймає звичайний
    /// TeamMembershipRequest. Так у складі не з'являється другого шляху
    /// опинитися, а сповіщення працюють самі собою: NotificationService
    /// проєктує саме цю таблицю.
    /// </summary>
    public class LftService : ILftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMembershipRequestService _membershipRequests;
        private readonly IMapper _mapper;

        public LftService(
            IUnitOfWork unitOfWork,
            IMembershipRequestService membershipRequests,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _membershipRequests = membershipRequests;
            _mapper = mapper;
        }

        public async Task<PagedResponse<TeamOpeningDto>> GetOpeningFeedAsync(PagedRequest request, int userId)
        {
            var player = await PlayerOfAsync(userId);

            var passes = _unitOfWork.LftPasses.GetQueryable();
            var pending = _unitOfWork.MembershipRequests.GetQueryable();

            // LftEligibility навмисно нічого не знає про стан вакансії — воно
            // описує кандидата, а не картку. Тож Open перевіряємо окремо, і
            // так само в другій стрічці.
            var query = _unitOfWork.TeamOpenings.GetQueryable()
                .Include(o => o.Team)
                .Where(o => o.Status == TeamOpeningStatus.Open)
                // Вимкнену команду звичайний шлях заявки не пропускає, тож і
                // тут не пропускаємо: вакансії самі собою не закриваються,
                // коли команду деактивують, і картка інакше лишалась би у
                // стрічці, ведучи в склад, куди прийняти вже не можна.
                .Where(o => o.Team.IsActive)
                // Власна команда у стрічці ні до чого, і капітан не змахує
                // по власній вакансії — LftPolicy.CanSwipeAsPlayer.
                .Where(o => o.Team.CaptainId != userId)
                .Where(o => !passes.Any(p => p.PlayerId == player.Id && p.TeamOpeningId == o.Id))
                .Where(o => !pending.Any(r => r.TeamId == o.TeamId
                                           && r.PlayerId == player.Id
                                           && r.Status == MembershipRequestStatus.Pending));

            // Окремою умовою, а не порівнянням із player.TeamId: гравець без
            // команди дав би `"TeamId" <> NULL`, тобто порожню стрічку.
            if (player.TeamId.HasValue)
            {
                var currentTeamId = player.TeamId.Value;
                query = query.Where(o => o.TeamId != currentTeamId);
            }

            // Правило одне на обидві стрічки; тут воно застосоване до самого
            // гравця, а не до кандидатів. Дисципліни звужуємо ще в SQL — це
            // та сама умова, що всередині предиката, лише дешевша.
            var games = player.GameProfiles
                .Where(p => p.IsLookingForTeam)
                .Select(p => p.Game)
                .Distinct()
                .ToList();

            query = query.Where(o => games.Contains(o.Game));

            var openings = await query
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
                .ToListAsync();

            // Ранг, роль і пояс звіряємо тим самим предикатом — скомпільованим,
            // бо гравець уже в пам'яті. Копії правила немає.
            var eligible = openings
                .Where(o => LftEligibility.Predicate(LftEligibility.From(o)).Compile()(player))
                .ToList();

            // ToPagedResponseAsync працює лише над IQueryable (і сам кличе
            // ApplyPaging), а тут список уже матеріалізовано, тож сторінку
            // ріжемо вручну. TotalCount — кількість придатних вакансій, а не
            // всіх відкритих: інакше в стрічці були б порожні сторінки.
            var page = eligible
                .Skip(request.Skip)
                .Take(request.Take)
                .ToList();

            return new PagedResponse<TeamOpeningDto>
            {
                Data = _mapper.Map<IEnumerable<TeamOpeningDto>>(page),
                TotalCount = eligible.Count,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResponse<PlayerSummaryDto>> GetCandidateFeedAsync(
            int openingId, PagedRequest request, int userId, bool isAdmin)
        {
            var opening = await OpeningAsync(openingId);

            if (!LftPolicy.CanManageOpening(opening.Team.CaptainId, userId, isAdmin))
            {
                throw new BusinessLogicException("Переглядати кандидатів може лише капітан команди");
            }

            if (opening.Status != TeamOpeningStatus.Open)
            {
                throw new BusinessLogicException("Вакансію вже закрито");
            }

            var passes = _unitOfWork.LftPasses.GetQueryable();
            var pending = _unitOfWork.MembershipRequests.GetQueryable();

            var query = _unitOfWork.Players.GetQueryable()
                .Where(LftEligibility.Predicate(LftEligibility.From(opening)))
                .Where(p => !passes.Any(pass => pass.PlayerId == p.Id && pass.TeamOpeningId == openingId))
                .Where(p => !pending.Any(r => r.TeamId == opening.TeamId
                                           && r.PlayerId == p.Id
                                           && r.Status == MembershipRequestStatus.Pending))
                .OrderByDescending(p => p.JoinedAt).ThenByDescending(p => p.Id);

            // Без ApplyPaging: ToPagedResponseAsync кличе його сам, а другий
            // виклик і сторінкував би двічі, і рахував би всього з уже
            // обрізаного запиту.
            return await query.ToPagedResponseAsync<Player, PlayerSummaryDto>(request, _mapper);
        }

        public async Task<SwipeResultDto> SwipeAsync(SwipeDto dto, int userId, bool isAdmin)
        {
            var opening = await OpeningAsync(dto.TeamOpeningId);

            if (opening.Status != TeamOpeningStatus.Open)
            {
                throw new BusinessLogicException("Вакансію вже закрито");
            }

            // Те саме, що перевіряє MembershipRequestService.CreateAsync:
            // у вимкнену команду не беруть. Вакансії команди, яку щойно
            // деактивували, лишаються Open, тож без цієї умови змах привів би
            // у склад повз правило, яке звичайний шлях заявки дотримує.
            if (!opening.Team.IsActive)
            {
                throw new BusinessLogicException("Команда неактивна");
            }

            // Бік визначає право, а не тіло запиту: назвати кандидата може лише
            // той, хто має право на вакансію. Решті це поле ігнорується, і змах
            // записується від їхнього власного імені — id з токена.
            var asCaptain = LftPolicy.CanManageOpening(opening.Team.CaptainId, userId, isAdmin);
            Player player;

            if (asCaptain && dto.PlayerId.HasValue)
            {
                player = await _unitOfWork.Players.GetQueryable()
                    .Include(p => p.GameProfiles)
                    .FirstOrDefaultAsync(p => p.Id == dto.PlayerId.Value)
                    ?? throw new EntityNotFoundException("Player", dto.PlayerId.Value);
            }
            else
            {
                // Капітан без кандидата змахує сам за себе — а власна вакансія
                // саме це й забороняє: інакше в складі опинився б гравець,
                // згоди якого ніхто не питав.
                if (!LftPolicy.CanSwipeAsPlayer(opening.Team.CaptainId, userId))
                {
                    throw new BusinessLogicException("Не можна відгукнутися на власну вакансію");
                }

                asCaptain = false;
                player = await PlayerOfAsync(userId);
            }

            if (!dto.Like)
            {
                await PassAsync(player.Id, opening.Id, userId);
                return new SwipeResultDto { Matched = false };
            }

            // Зустрічна заявка вже є — приймаємо її, а не створюємо другу.
            // ResponderUserId сам ставить капітана відповідачем на Application,
            // а гравця — на Invite, тож окремої перевірки тут не треба.
            //
            // Шукаємо за парою (команда, гравець), а не за вакансією, — саме
            // так пару бачить часткова унікальність у team_membership_requests
            // і саме так її бачить NotificationService. Тому взаємна згода в
            // принципі може закрити іншу вакансію тієї самої команди, ніж та,
            // по якій щойно змахнули: закриється та, з якої постав запит, —
            // тобто та, на яку гравець насправді погоджувався. Це навмисно, і
            // не варто «виправляти» це на пошук за TeamOpeningId: тоді на пару
            // з'явився б другий Pending-рядок, якого індекс не пустить.
            //
            // Перевірку придатності нижче цей шлях обходить свідомо: рядок уже
            // існує, обидві сторони на нього погодились, і той самий рядок
            // приймається зі сповіщень без жодних умов. Умова тут дала б дві
            // різні відповіді на один і той самий запит.
            var counterpart = await _unitOfWork.MembershipRequests.GetQueryable()
                .FirstOrDefaultAsync(r => r.TeamId == opening.TeamId
                                       && r.PlayerId == player.Id
                                       && r.Status == MembershipRequestStatus.Pending);

            if (counterpart != null)
            {
                var matched = await _membershipRequests.AcceptAsync(counterpart.Id, userId, isAdmin);
                return new SwipeResultDto { Matched = true, MembershipRequestId = matched.Id };
            }

            // Новий запит — це згода з чистого аркуша, тож придатність
            // звіряємо тим самим правилом, що й обидві стрічки. Стрічка
            // непридатну картку не покаже, але endpoint кличуть і напряму, а
            // капітан ще й називає гравця сам. Змах ліворуч цієї перевірки не
            // проходить навмисно: він лише прибирає картку й зіпсувати нічого
            // не може.
            if (!LftEligibility.Predicate(LftEligibility.From(opening)).Compile()(player))
            {
                throw new BusinessLogicException("Гравець не підходить під цю вакансію");
            }

            var request = new TeamMembershipRequest
            {
                TeamId = opening.TeamId,
                PlayerId = player.Id,
                TeamOpeningId = opening.Id,
                Direction = asCaptain
                    ? MembershipRequestDirection.Invite
                    : MembershipRequestDirection.Application,
                Status = MembershipRequestStatus.Pending,
                InitiatedByUserId = userId
            };

            try
            {
                await _unitOfWork.MembershipRequests.AddAsync(request);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Часткова унікальність спрацювала: назустріч устиг інший змах.
                // Невдалу вставку спершу відчіплюємо — після провалу вона
                // лишається Added, і наступний SaveChanges усередині
                // AcceptAsync спробував би її знову, поваливши й прийняття.
                _unitOfWork.MembershipRequests.Remove(request);

                var raced = await _unitOfWork.MembershipRequests.GetQueryable()
                    .FirstOrDefaultAsync(r => r.TeamId == opening.TeamId
                                           && r.PlayerId == player.Id
                                           && r.Status == MembershipRequestStatus.Pending);

                // Порушення було не тим, на яке ми розраховували.
                if (raced == null)
                {
                    throw;
                }

                var matched = await _membershipRequests.AcceptAsync(raced.Id, userId, isAdmin);
                return new SwipeResultDto { Matched = true, MembershipRequestId = matched.Id };
            }

            return new SwipeResultDto { Matched = false, MembershipRequestId = request.Id };
        }

        private async Task PassAsync(int playerId, int openingId, int userId)
        {
            var already = await _unitOfWork.LftPasses.GetQueryable()
                .AnyAsync(p => p.PlayerId == playerId && p.TeamOpeningId == openingId);

            // Повторний змах ліворуч — не помилка: картки все одно вже немає.
            if (already)
            {
                return;
            }

            var pass = new LftPass
            {
                PlayerId = playerId,
                TeamOpeningId = openingId,
                PassedByUserId = userId
            };

            try
            {
                await _unitOfWork.LftPasses.AddAsync(pass);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Той самий змах устиг записатися паралельно — унікальність
                // пари це й ловить. Відчіплюємо невдалу вставку й мовчимо,
                // але лише якщо рядок справді вже є: інакше помилка інша.
                _unitOfWork.LftPasses.Remove(pass);

                var raced = await _unitOfWork.LftPasses.GetQueryable()
                    .AnyAsync(p => p.PlayerId == playerId && p.TeamOpeningId == openingId);

                if (!raced)
                {
                    throw;
                }
            }
        }

        private async Task<TeamOpening> OpeningAsync(int id) =>
            await _unitOfWork.TeamOpenings.GetQueryable()
                .Include(o => o.Team)
                .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new EntityNotFoundException("TeamOpening", id);

        private async Task<Player> PlayerOfAsync(int userId) =>
            await _unitOfWork.Players.GetQueryable()
                .Include(p => p.GameProfiles)
                .FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new BusinessLogicException("У вашого акаунта немає профілю гравця");
    }
}
