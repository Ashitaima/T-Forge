namespace TForge.Common
{
    /// <summary>
    /// Правила одного ходу — чиста функція без EF, як решта у Common.
    ///
    /// Сервіс читає рядки, складає State і питає, чи можна. Уся логіка «чий
    /// хід, що ще вільне, чи це взагалі існує» живе тут, і саме тому її можна
    /// перевірити модульними тестами, не піднімаючи базу.
    ///
    /// Одна тонкість, яку легко втратити: сторона з пулу не вибуває. «CT»
    /// обирають на кожній карті наново, а забанену карту — ніколи. За це
    /// відповідає DraftActions.ConsumesFromPool.
    /// </summary>
    public static class DraftEngine
    {
        public record StepView(int StepIndex, string Action, int? ActingTeamId, string? ChosenValue);

        public record State(string Game, string Status, int CurrentStepIndex, IReadOnlyList<StepView> Steps);

        /// <summary>Reason заповнена лише тоді, коли відмовлено.</summary>
        public record Verdict(bool IsAllowed, string? Reason);

        private static readonly Verdict Allowed = new(true, null);

        private static Verdict Refuse(string reason) => new(false, reason);

        /// <summary>
        /// З чого взагалі обирають на кроці такої дії.
        ///
        /// Дисципліна драфтить або карти, або героїв — ніколи і те, і те, тож
        /// одного «є карти?» вистачає, щоб розвести ці два випадки.
        /// </summary>
        public static IReadOnlyList<string> CatalogFor(string game, string action)
        {
            if (action == DraftActions.Side)
            {
                return GameSides.ForGame(game);
            }

            return GameMaps.HasMaps(game) ? GameMaps.ForGame(game) : GameHeroes.ForGame(game);
        }

        /// <summary>
        /// Що ще можна взяти на цьому кроці: каталог мінус уже витрачене.
        /// Для Decider це рівно одна карта — та, що лишилась.
        /// </summary>
        public static IReadOnlyList<string> AvailableValues(State state, int stepIndex)
        {
            var step = state.Steps.FirstOrDefault(s => s.StepIndex == stepIndex);
            if (step == null)
            {
                return Array.Empty<string>();
            }

            var catalog = CatalogFor(state.Game, step.Action);

            if (!DraftActions.ConsumesFromPool(step.Action))
            {
                return catalog;
            }

            var taken = state.Steps
                .Where(s => s.ChosenValue != null && DraftActions.ConsumesFromPool(s.Action))
                .Select(s => s.ChosenValue!)
                .ToHashSet();

            return catalog.Where(value => !taken.Contains(value)).ToList();
        }

        /// <summary>Чи можна застосувати цей вибір саме зараз.</summary>
        public static Verdict CanApply(State state, int stepIndex, string? value)
        {
            if (state.Status != DraftStatuses.InProgress)
            {
                return Refuse("Драфт не триває");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Refuse("Не вказано вибір");
            }

            // Застарілий крок: клієнт відповідає на хід, який уже зроблено —
            // найчастіше через таймер, що спрацював за мить до натискання.
            if (stepIndex != state.CurrentStepIndex)
            {
                return Refuse("Цей хід уже зроблено");
            }

            var step = state.Steps.FirstOrDefault(s => s.StepIndex == stepIndex);
            if (step == null)
            {
                return Refuse("Такого кроку в драфті немає");
            }

            if (step.ChosenValue != null)
            {
                return Refuse("Цей хід уже зроблено");
            }

            if (!AvailableValues(state, stepIndex).Contains(value))
            {
                return Refuse("Цей варіант недоступний");
            }

            return Allowed;
        }

        public static bool IsLastStep(State state, int stepIndex) =>
            state.Steps.Count > 0 && stepIndex == state.Steps.Max(s => s.StepIndex);
    }
}
