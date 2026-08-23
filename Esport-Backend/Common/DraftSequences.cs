namespace TForge.Common
{
    /// <summary>
    /// Порядок кроків драфту — чиста таблиця, без EF і без сервісів.
    ///
    /// A — команда, що виграла жеребкування (MatchDraft.FirstTurnTeamId) і
    /// має перший пік, B — друга. Виконавця кожного кроку видно вже тут, і це
    /// не випадково: рядки кроків створюються всі одразу, тож розгалужень, які
    /// залежать від уже зробленого вибору, тут бути не може.
    ///
    /// Саме тому жереб вирішує лише перший пік, а сторону винесено в окремий
    /// крок: справжнє правило «переможець жеребкування обирає АБО сторону, АБО
    /// перший пік» розвертає виконавців усіх наступних кроків, і наперед такі
    /// рядки не створити.
    ///
    /// Порядки прив'язані до патчів і час від часу міняються. Файл один і
    /// чистий саме тому: перебудувати послідовність — це правка в одному
    /// місці, а вже зіграні драфти лишаються незмінними, бо їхні рядки
    /// створено наперед.
    /// </summary>
    public static class DraftSequences
    {
        /// <summary>
        /// Один крок. IsFirstTurnTeam == null означає, що виконавця немає —
        /// так буває лише в Decider, який сервер проставляє сам.
        /// </summary>
        public record SequenceStep(string Action, bool? IsFirstTurnTeam);

        private static SequenceStep Ban(bool a) => new(DraftActions.Ban, a);
        private static SequenceStep Pick(bool a) => new(DraftActions.Pick, a);
        private static SequenceStep Side(bool a) => new(DraftActions.Side, a);
        private static readonly SequenceStep Decider = new(DraftActions.Decider, null);

        private const bool A = true;
        private const bool B = false;

        // ---- Вето карт: пул із семи ----
        //
        // Бани + піки + вирішальна = рівно 7 у кожному форматі, інакше або
        // лишається зайва карта, або кроків більше, ніж є що обирати.
        //
        // Сторону на обраній карті бере та команда, яка її не обирала; на
        // вирішальній — та, яка не банила останньою (у всіх форматах це A).

        private static readonly SequenceStep[] MapsBo1 =
        {
            Ban(A), Ban(B), Ban(A), Ban(B), Ban(A), Ban(B),
            Decider, Side(A)
        };

        private static readonly SequenceStep[] MapsBo3 =
        {
            Ban(A), Ban(B),
            Pick(A), Side(B),
            Pick(B), Side(A),
            Ban(A), Ban(B),
            Decider, Side(A)
        };

        private static readonly SequenceStep[] MapsBo5 =
        {
            Ban(A), Ban(B),
            Pick(A), Side(B),
            Pick(B), Side(A),
            Pick(A), Side(B),
            Pick(B), Side(A),
            Decider, Side(A)
        };

        // ---- Dota 2, Captains Mode ----
        //
        // Порядок патча 7.34 і пізніших: команда з першим піком банить 3-2-2,
        // команда з другим — 4-1-2; піки в обох 1-3-1. Разом 14 банів (по 7) і
        // 10 піків (по 5).
        //
        // Розподіл по фазах узято з правил гри; чергування всередині фази
        // підібрано так, щоб дати саме ці кількості. Якщо Valve змінить
        // порядок, правити треба тут — і тільки тут.
        //
        // Сторону обирає B: перший пік уже дістався A.
        private static readonly SequenceStep[] Dota2Cm =
        {
            Side(B),
            // Бани, фаза 1 — сім: A тричі, B чотири рази.
            Ban(B), Ban(A), Ban(B), Ban(A), Ban(B), Ban(A), Ban(B),
            // Піки, фаза 1 — по одному.
            Pick(A), Pick(B),
            // Бани, фаза 2 — три: A двічі, B один раз.
            Ban(A), Ban(B), Ban(A),
            // Піки, фаза 2 — по три.
            Pick(B), Pick(A), Pick(A), Pick(B), Pick(B), Pick(A),
            // Бани, фаза 3 — по два.
            Ban(A), Ban(B), Ban(A), Ban(B),
            // Піки, фаза 3 — по одному.
            Pick(B), Pick(A)
        };

        // ---- League of Legends, турнірний драфт ----
        //
        // Класичний порядок: сині банять першими, далі
        // B1 R1 R2 B2 B3 R3, другий бан-фаза починає червона сторона,
        // і завершують R4 B4 B5 R5. Разом 10 банів і 10 піків, порівну.
        private static readonly SequenceStep[] LolDraft =
        {
            Side(B),
            Ban(A), Ban(B), Ban(A), Ban(B), Ban(A), Ban(B),
            Pick(A), Pick(B), Pick(B), Pick(A), Pick(A), Pick(B),
            Ban(B), Ban(A), Ban(B), Ban(A),
            Pick(B), Pick(A), Pick(A), Pick(B)
        };

        private static readonly SequenceStep[] None = Array.Empty<SequenceStep>();

        /// <summary>
        /// Кроки для дисципліни й формату. Порожньо — драфту немає.
        ///
        /// У Dota 2 і LoL формат не впливає ні на що: драфт один на матч, і він
        /// описує першу гру серії. У справжній BO3 героїв драфтять перед кожною
        /// грою, але система не веде рахунок окремих ігор усередині серії, тож
        /// і драфтів у неї рівно стільки ж, скільки матчів.
        /// </summary>
        public static IReadOnlyList<SequenceStep> For(string? game, string? format) => game switch
        {
            Games.CS2 or Games.Valorant => format switch
            {
                "BO1" => MapsBo1,
                "BO3" => MapsBo3,
                "BO5" => MapsBo5,
                _ => None
            },
            Games.Dota2 => Dota2Cm,
            Games.LeagueOfLegends => LolDraft,
            _ => None
        };

        public static bool HasDraft(string? game) => Games.IsValid(game);
    }
}
