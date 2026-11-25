using System.Diagnostics;
using System.Text;
using TForge.Common;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Запускає ml/chess/train.py.
    ///
    /// Шелла немає: ProcessStartInfo.ArgumentList передає кожен аргумент
    /// окремо, тож жодне ім'я файлу нічого не може інжектнути — та сама
    /// обережність, що в AvatarRules, де тип визначається за байтами, а не за
    /// тим, що сказав клієнт. Шлях до файлу все одно будує сервер.
    ///
    /// Відсутній інтерпретатор не валить застосунок: IsAvailable стає false, і
    /// ендпоїнти віддають 200 { available: false, reason }, як це вже робить
    /// прогноз без моделей.
    /// </summary>
    public sealed class ChessTrainerProcess : IChessTrainerProcess
    {
        private const int LogLimit = 16 * 1024;

        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly TimeSpan _timeout;
        private readonly ILogger<ChessTrainerProcess> _logger;

        public bool IsAvailable => UnavailableReason == null;
        public string? UnavailableReason { get; }

        public ChessTrainerProcess(
            string pythonPath, string scriptPath, TimeSpan timeout, ILogger<ChessTrainerProcess> logger)
        {
            _pythonPath = pythonPath;
            _scriptPath = scriptPath;
            _timeout = timeout;
            _logger = logger;

            UnavailableReason = Probe(pythonPath, scriptPath);
            if (UnavailableReason != null)
            {
                _logger.LogWarning(
                    "Навчання моделей недоступне: {Reason} (python: {Python}, скрипт: {Script})",
                    UnavailableReason, pythonPath, scriptPath);
            }
        }

        private static string? Probe(string pythonPath, string scriptPath)
        {
            if (!File.Exists(pythonPath))
            {
                return "не знайдено інтерпретатор Python зі ml/chess/.venv";
            }

            if (!File.Exists(scriptPath))
            {
                return "не знайдено скрипт ml/chess/train.py";
            }

            return null;
        }

        public async Task<ChessDatasetSummary> ValidateAsync(string csvPath, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
            {
                return ChessDatasetSummary.Rejected($"Перевірка недоступна: {UnavailableReason}");
            }

            var run = await RunAsync(new[] { _scriptPath, "--validate", "--data", csvPath }, cancellationToken);

            // Ненульовий код — це зламаний інструментарій, а не поганий файл:
            // --validate навмисно виходить нулем навіть коли відмовляє.
            if (run.ExitCode != 0)
            {
                return ChessDatasetSummary.Rejected(
                    $"Перевірка не виконалася (код {run.ExitCode}). {Tail(run.Error)}");
            }

            return ChessDatasetSummary.Parse(run.Output.Trim());
        }

        public async Task<TrainerResult> TrainAsync(
            string csvPath, string outDirectory, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
            {
                return new TrainerResult(false, string.Empty, $"Навчання недоступне: {UnavailableReason}");
            }

            Directory.CreateDirectory(outDirectory);

            // --no-install обов'язковий: без нього train.py копіює результат у
            // MlModels/chess і перезаписує вбудовану модель разом із
            // parity-фікстурою.
            var run = await RunAsync(
                new[] { _scriptPath, "--data", csvPath, "--out", outDirectory, "--no-install" },
                cancellationToken);

            var log = Trim(run.Output + (run.Error.Length > 0 ? "\n--- stderr ---\n" + run.Error : string.Empty));

            if (run.TimedOut)
            {
                return new TrainerResult(false, log,
                    $"Перевищено ліміт часу ({_timeout.TotalMinutes:0} хв)");
            }

            if (run.ExitCode != 0)
            {
                return new TrainerResult(false, log, $"Навчання завершилося з кодом {run.ExitCode}");
            }

            return new TrainerResult(true, log, null);
        }

        private sealed record ProcessRun(int ExitCode, string Output, string Error, bool TimedOut);

        private async Task<ProcessRun> RunAsync(string[] arguments, CancellationToken cancellationToken)
        {
            var info = new ProcessStartInfo
            {
                FileName = _pythonPath,
                // Робоча тека — ml/chess: train.py імпортує features і datasets
                // як модулі поруч із собою, і відносний data/raw/... теж від неї.
                WorkingDirectory = Path.GetDirectoryName(_scriptPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Читати UTF-8 мало — дочірній Python має ним ще й писати. Без
            // цього він бере системну сторінку (на Windows cp1252), і друк
            // українського повідомлення про проблему падає з
            // UnicodeEncodeError: вердикт «поганий файл» перетворювався на
            // ненульовий код виходу, тобто на «зламаний інструментарій».
            info.Environment["PYTHONIOENCODING"] = "utf-8";

            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = info };
            process.Start();

            // Читаємо обидва потоки паралельно з очікуванням: послідовне
            // читання заклинює процес, щойно другий буфер заповниться.
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                return new ProcessRun(
                    -1, await Safe(output), await Safe(error),
                    TimedOut: !cancellationToken.IsCancellationRequested);
            }

            return new ProcessRun(process.ExitCode, await Safe(output), await Safe(error), TimedOut: false);
        }

        private void Kill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
            {
                _logger.LogWarning(exception, "Не вдалося зупинити процес навчання");
            }
        }

        private static async Task<string> Safe(Task<string> stream)
        {
            try
            {
                return await stream;
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException)
            {
                return string.Empty;
            }
        }

        private static string Tail(string text) => text.Length <= 500 ? text : text[^500..];

        private static string Trim(string log) => log.Length <= LogLimit ? log : log[^LogLimit..];
    }
}
