using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Два стартові обов'язки, обидва неможливі в конструкторі синглтона:
    ///
    /// 1. Застосувати активну версію моделі. ChessModelRunner — singleton, а
    ///    DbContext — scoped, тож «яку версію взяти» читається тут, а не там.
    ///    Немає активної — лишається вбудована, завантажена конструктором.
    ///
    /// 2. Закрити запуски, що залишилися Running. Їхній процес помер разом із
    ///    хостом, і відрізнити такий рядок від живого після перезапуску
    ///    неможливо: кожен Running на старті — за визначенням покинутий.
    ///    Без цього черга б заклинила назавжди, бо вона не бере наступний,
    ///    поки щось «виконується». Саме тому ця служба реєструється ПЕРЕД
    ///    ChessTrainingRunnerService — щоб ремонт стався до першого такту.
    /// </summary>
    public class ChessModelActivationService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IChessModelRunner _runner;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChessModelActivationService> _logger;

        public ChessModelActivationService(
            IServiceScopeFactory scopeFactory,
            IChessModelRunner runner,
            IWebHostEnvironment environment,
            ILogger<ChessModelActivationService> logger)
        {
            _scopeFactory = scopeFactory;
            _runner = runner;
            _environment = environment;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await RepairOrphanedRunsAsync(unitOfWork, cancellationToken);
            await ApplyActiveVersionAsync(unitOfWork, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task RepairOrphanedRunsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        {
            var orphaned = await unitOfWork.ChessTrainingRuns.GetQueryable()
                .Where(r => r.Status == TrainingRunStatuses.Running)
                .ToListAsync(cancellationToken);

            if (orphaned.Count == 0)
            {
                return;
            }

            foreach (var run in orphaned)
            {
                run.Status = TrainingRunStatuses.Failed;
                run.FinishedAt = DateTime.UtcNow;
                run.ErrorMessage = "Навчання перервано зупинкою сервера";
                unitOfWork.ChessTrainingRuns.Update(run);
            }

            await unitOfWork.SaveChangesAsync();
            _logger.LogWarning("Закрито {Count} покинутих запусків навчання", orphaned.Count);
        }

        private async Task ApplyActiveVersionAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        {
            var active = await unitOfWork.ChessTrainingRuns.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IsActive, cancellationToken);

            if (active?.ModelPath == null)
            {
                return;
            }

            var directory = Path.Combine(_environment.ContentRootPath, active.ModelPath);
            if (!_runner.Load(directory, active.Id))
            {
                // Файли активної версії зникли або зіпсувалися. Вбудована
                // модель уже завантажена конструктором, тож прогноз працює —
                // просто не тією версією, яку обрали. Прапорець у базі не
                // чіпаємо: адміністратор має побачити розбіжність, а не
                // виявити, що вибір тихо скинувся.
                _logger.LogError(
                    "Активна версія {RunId} не завантажилася з {Directory}; працює вбудована модель",
                    active.Id, directory);
            }
        }
    }
}
