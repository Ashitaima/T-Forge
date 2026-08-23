using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class ScheduleRulesTests
{
    // ---- Просте порівняння ----

    [Fact]
    public void IsInFuture_LaterToday_IsTrue()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(ScheduleRules.IsInFuture(now.AddHours(3), now));
    }

    [Fact]
    public void IsInFuture_Yesterday_IsFalse()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(ScheduleRules.IsInFuture(now.AddDays(-1), now));
    }

    // Матч, що вже почався годину тому, призначити не можна — саме через це
    // правило й з'явилося.
    [Fact]
    public void IsInFuture_AnHourAgo_IsFalse()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(ScheduleRules.IsInFuture(now.AddHours(-1), now));
    }

    // ---- Запас на годинники ----

    // «Через хвилину» мусить проходити: поки форма надсилає запит, цей час
    // устигає стати минулим.
    [Fact]
    public void IsInFuture_JustNow_IsTrueWithinTolerance()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(ScheduleRules.IsInFuture(now.AddMinutes(-1), now));
    }

    [Fact]
    public void IsInFuture_BeyondTolerance_IsFalse()
    {
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(ScheduleRules.IsInFuture(now - ScheduleRules.ClockTolerance.Add(TimeSpan.FromMinutes(1)), now));
    }

    // ---- Одна шкала часу ----

    // Та сама помилка, заради якої правило винесли сюди: значення приходить у
    // місцевому часі, а порівнюють його з UTC. Тест не залежить від зсуву
    // машини, бо обидві сторони виведені з одного й того самого моменту.
    [Fact]
    public void IsInFuture_LocalValue_ComparedInUtc()
    {
        var localNoon = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Local);
        var nowUtc = localNoon.ToUniversalTime().AddHours(-1);

        Assert.True(ScheduleRules.IsInFuture(localNoon, nowUtc));
    }

    [Fact]
    public void IsInFuture_LocalValueInThePast_IsFalse()
    {
        var localNoon = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Local);
        var nowUtc = localNoon.ToUniversalTime().AddHours(1);

        Assert.False(ScheduleRules.IsInFuture(localNoon, nowUtc));
    }

    // Саме такий Kind дає System.Text.Json для «2026-09-01T12:00» — рядка,
    // який надсилає DateTimePicker. Тлумачимо його як місцевий, як і
    // UtcDateTimeConverter, інакше два шляхи розійшлися б.
    [Fact]
    public void IsInFuture_UnspecifiedKind_IsTreatedAsLocal()
    {
        var unspecified = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var nowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Local).ToUniversalTime().AddHours(-1);

        Assert.True(ScheduleRules.IsInFuture(unspecified, nowUtc));
    }

    [Fact]
    public void PastMessage_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ScheduleRules.PastMessage));
    }
}
