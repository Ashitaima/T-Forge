using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class DraftPolicyTests
{
    private const int HomeTeam = 1;
    private const int AwayTeam = 2;
    private const int HomeCaptain = 10;
    private const int AwayCaptain = 20;
    private const int Stranger = 30;

    private static DraftPolicy.Context Friendly() =>
        new(HomeTeam, AwayTeam, HomeCaptain, AwayCaptain, null);

    private static DraftPolicy.Context Tournament() =>
        new(HomeTeam, AwayTeam, HomeCaptain, AwayCaptain, 7);

    private static DraftPolicy.Context Open() =>
        new(HomeTeam, null, HomeCaptain, null, null);

    // ---- Ходить лише той капітан, чий хід ----

    [Fact]
    public void CanAct_CaptainOfActingTeam_May() =>
        Assert.True(DraftPolicy.CanAct(Friendly(), HomeCaptain, HomeTeam));

    [Fact]
    public void CanAct_CaptainOfTheOtherTeam_MayNot() =>
        Assert.False(DraftPolicy.CanAct(Friendly(), AwayCaptain, HomeTeam));

    [Fact]
    public void CanAct_Stranger_MayNot() =>
        Assert.False(DraftPolicy.CanAct(Friendly(), Stranger, HomeTeam));

    [Fact]
    public void CanAct_NoActingTeam_MayNot() =>
        // Крок Decider не належить нікому — його проставляє сервер.
        Assert.False(DraftPolicy.CanAct(Friendly(), HomeCaptain, null));

    [Fact]
    public void CanAct_AwayCaptainOnAwayTurn_May() =>
        Assert.True(DraftPolicy.CanAct(Friendly(), AwayCaptain, AwayTeam));

    [Fact]
    public void CanAct_TeamOutsideTheMatch_MayNot() =>
        Assert.False(DraftPolicy.CanAct(Friendly(), HomeCaptain, 999));

    // ---- Адміністратор не банить. Це навмисно. ----

    [Fact]
    public void CanAct_TakesNoAdminFlagAtAll()
    {
        // Підпис CanAct не приймає isAdmin узагалі: адміністратор, який банить
        // за команду, лишає запис, що бреше про те, що сталося. Якщо колись
        // з'явиться перевантаження з isAdmin, цей тест має впасти першим.
        var parameters = typeof(DraftPolicy)
            .GetMethod(nameof(DraftPolicy.CanAct))!
            .GetParameters();

        Assert.DoesNotContain(parameters, p => p.Name == "isAdmin");
    }

    [Fact]
    public void CanAct_AdminIsJustAnotherStranger() =>
        Assert.False(DraftPolicy.CanAct(Tournament(), Stranger, HomeTeam));

    // ---- Керування драфтом ----

    [Fact]
    public void CanManage_Admin_May() =>
        Assert.True(DraftPolicy.CanManage(Tournament(), Stranger, isAdmin: true, isOrganizer: false));

    [Fact]
    public void CanManage_Organizer_May() =>
        Assert.True(DraftPolicy.CanManage(Tournament(), Stranger, isAdmin: false, isOrganizer: true));

    [Fact]
    public void CanManage_HomeCaptain_May() =>
        Assert.True(DraftPolicy.CanManage(Friendly(), HomeCaptain, isAdmin: false, isOrganizer: false));

    [Fact]
    public void CanManage_AwayCaptain_May() =>
        Assert.True(DraftPolicy.CanManage(Friendly(), AwayCaptain, isAdmin: false, isOrganizer: false));

    [Fact]
    public void CanManage_Stranger_MayNot() =>
        Assert.False(DraftPolicy.CanManage(Friendly(), Stranger, isAdmin: false, isOrganizer: false));

    [Fact]
    public void CanManage_CaptainOfTournamentMatch_May() =>
        // Капітан ставить драфт і у своєму турнірному матчі: драфт нічого не
        // присуджує, на відміну від рахунку.
        Assert.True(DraftPolicy.CanManage(Tournament(), HomeCaptain, isAdmin: false, isOrganizer: false));

    [Fact]
    public void CanManage_OpenMatchHomeCaptain_May() =>
        Assert.True(DraftPolicy.CanManage(Open(), HomeCaptain, isAdmin: false, isOrganizer: false));

    // ---- Відкритий матч ----

    [Fact]
    public void CanAct_OpenMatch_NoOpponentToAct() =>
        Assert.False(DraftPolicy.CanAct(Open(), HomeCaptain, AwayTeam));

    [Fact]
    public void CanAct_OpenMatchHomeTurn_StillWorks() =>
        Assert.True(DraftPolicy.CanAct(Open(), HomeCaptain, HomeTeam));

    // ---- Хто капітан якої команди ----

    [Fact]
    public void CaptainOf_HomeTeam_IsHomeCaptain() =>
        Assert.Equal(HomeCaptain, DraftPolicy.CaptainOf(Friendly(), HomeTeam));

    [Fact]
    public void CaptainOf_AwayTeam_IsAwayCaptain() =>
        Assert.Equal(AwayCaptain, DraftPolicy.CaptainOf(Friendly(), AwayTeam));

    [Fact]
    public void CaptainOf_UnknownTeam_IsNull() => Assert.Null(DraftPolicy.CaptainOf(Friendly(), 999));

    [Fact]
    public void CaptainOf_OpenMatchAwaySlot_IsNull() =>
        Assert.Null(DraftPolicy.CaptainOf(Open(), AwayTeam));
}
