namespace TForge.Common
{
    /// <summary>
    /// Хто й що може зробити з датасетами й запусками навчання.
    ///
    /// Чисті функції без EF — сервіс читає дані, рішення ухвалює цей клас, як
    /// FriendlyMatchPolicy і DraftPolicy.
    ///
    /// Параметра isAdmin тут немає взагалі, і це не недогляд: усі ці дії
    /// рольовий атрибут уже віддав адміністраторові й нікому іншому, тож
    /// виключати з правила власності нікого не потрібно.
    /// </summary>
    public static class ChessTrainingPolicy
    {
        /// <summary>
        /// Один запуск одночасно. Два навчання б'ються за процесор, і обидва
        /// хочуть перемкнути активну модель — тож черга рівно на одного.
        /// </summary>
        public static bool CanQueue(bool hasUnfinishedRun) => !hasUnfinishedRun;

        /// <summary>Активувати можна лише те, що справді має файли моделі.</summary>
        public static bool CanActivate(string status) => status == TrainingRunStatuses.Succeeded;

        /// <summary>
        /// Активну версію не видаляють — інакше прогноз лишився б із
        /// посиланням на зниклі файли. Незавершену теж: її процес ще живий.
        /// </summary>
        public static bool CanDeleteRun(string status, bool isActive) =>
            !isActive && TrainingRunStatuses.IsFinished(status);

        /// <summary>
        /// Датасет тримають його запуски: метрики без вихідних даних нічого не
        /// означають, тож спершу видаляють запуски.
        /// </summary>
        public static bool CanDeleteDataset(bool hasRuns) => !hasRuns;
    }
}
