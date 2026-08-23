using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Закриває прострочені ходи драфту.
    ///
    /// Без цього «живий» драфт живий лише поки на нього хтось дивиться:
    /// капітан, який закрив вкладку, спиняв би матч назавжди. Тому годинник
    /// серверний, і рухає його саме ця служба.
    ///
    /// Хід вона робить тим самим шляхом, що й людина — через
    /// DraftService.ApplyStepAsync із перевіркою індексу кроку. Якщо капітан
    /// устиг за мить до цього, перевірка відмовить, і це правильна відповідь,
    /// а не помилка.
    ///
    /// Це опитування, і воно крутиться завжди. На цих обсягах воно дешевше за
    /// будь-яку альтернативу.
    /// </summary>
    public class DraftTimeoutService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DraftTimeoutService> _logger;

        public DraftTimeoutService(IServiceScopeFactory scopeFactory, ILogger<DraftTimeoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Tick);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    // Збій одного такту не має валити службу: наступної секунди
                    // спробуємо знову.
                    _logger.LogError(exception, "Не вдалося закрити прострочений хід драфту");
                }
            }
        }

        private async Task TickAsync(CancellationToken stoppingToken)
        {
            // DbContext і сервіси — scoped, тож свій скоуп на такт.
            using var scope = _scopeFactory.CreateScope();
            var drafts = scope.ServiceProvider.GetRequiredService<IDraftService>();

            var expired = await drafts.FindExpiredDraftIdsAsync(DateTime.UtcNow);

            foreach (var draftId in expired)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await drafts.AutoPickAsync(draftId);
            }
        }
    }
}
