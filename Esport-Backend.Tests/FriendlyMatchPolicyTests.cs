using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class FriendlyMatchPolicyTests
{
    private const int HomeCaptain = 10;
    private const int AwayCaptain = 20;
    private const int Stranger = 30;

    private static FriendlyMatchPolicy.Context Friendly() => new(null, HomeCaptain, AwayCaptain);

    private static FriendlyMatchPolicy.Context Tournament() => new(7, HomeCaptain, AwayCaptain);

    // ---- Що таке товариський матч ----

    [Fact]
    public void IsFriendly_NoTournament_IsTrue() => Assert.True(FriendlyMatchPolicy.IsFriendly(Friendly()));

    [Fact]
    public void IsFriendly_WithTournament_IsFalse() => Assert.False(FriendlyMatchPolicy.IsFriendly(Tournament()));

    // ---- Товариський матч ведуть капітани ----

    [Fact]
    public void CanManage_FriendlyHomeCaptain_May()
    {
        Assert.True(FriendlyMatchPolicy.CanManage(Friendly(), HomeCaptain, isAdmin: false, isOrganizer: false));
    }

    [Fact]
    public void CanManage_FriendlyAwayCaptain_May()
    {
        Assert.True(FriendlyMatchPolicy.CanManage(Friendly(), AwayCaptain, isAdmin: false, isOrganizer: false));
    }

    [Fact]
    public void CanManage_FriendlyStranger_MayNot()
    {
        Assert.False(FriendlyMatchPolicy.CanManage(Friendly(), Stranger, isAdmin: false, isOrganizer: false));
    }

    // ---- Турнірний матч капітанам не належить ----
    // Інакше капітан міг би сам собі зарахувати перемогу в турнірі.

    [Fact]
    public void CanManage_TournamentMatchCaptain_MayNot()
    {
        Assert.False(FriendlyMatchPolicy.CanManage(Tournament(), HomeCaptain, isAdmin: false, isOrganizer: false));
    }

    // ---- Права організатора й адміністратора не звужуються ----

    [Fact]
    public void CanManage_OrganizerMayManageTournamentMatch()
    {
        Assert.True(FriendlyMatchPolicy.CanManage(Tournament(), Stranger, isAdmin: false, isOrganizer: true));
    }

    [Fact]
    public void CanManage_OrganizerMayManageFriendly()
    {
        Assert.True(FriendlyMatchPolicy.CanManage(Friendly(), Stranger, isAdmin: false, isOrganizer: true));
    }

    [Fact]
    public void CanManage_AdminMayManageEither()
    {
        Assert.True(FriendlyMatchPolicy.CanManage(Tournament(), Stranger, isAdmin: true, isOrganizer: false));
        Assert.True(FriendlyMatchPolicy.CanManage(Friendly(), Stranger, isAdmin: true, isOrganizer: false));
    }

    // Обидві команди можуть мати одного капітана лише в тестових даних,
    // але правило має лишатися однозначним.
    [Fact]
    public void CanManage_SameCaptainBothSides_May()
    {
        var context = new FriendlyMatchPolicy.Context(null, HomeCaptain, HomeCaptain);
        Assert.True(FriendlyMatchPolicy.CanManage(context, HomeCaptain, isAdmin: false, isOrganizer: false));
    }
}
