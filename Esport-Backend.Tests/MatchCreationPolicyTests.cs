using TForge.Common;

namespace TForge.Tests;

/// <summary>
/// Право створити матч. Найважливіше тут — те, що роль Organizer не відчиняє
/// чужий турнір: саме таку помилку вже виправляла TournamentOwnershipPolicy,
/// і повторювати її на створенні матчів не варто.
/// </summary>
public class MatchCreationPolicyTests
{
    private const int Admin = 1;
    private const int Owner = 2;
    private const int OtherOrganizer = 3;
    private const int HomeCaptain = 4;
    private const int AwayCaptain = 5;
    private const int Stranger = 6;

    private static MatchCreationPolicy.Context Friendly() =>
        new(TournamentId: null, TournamentOrganizerUserId: null, HomeCaptain, AwayCaptain);

    private static MatchCreationPolicy.Context Tournament() =>
        new(TournamentId: 10, TournamentOrganizerUserId: Owner, HomeCaptain, AwayCaptain);

    // ---------- Товариський матч ----------

    [Theory]
    [InlineData(HomeCaptain)]
    [InlineData(AwayCaptain)]
    public void Friendly_EitherCaptainMayCreate(int userId)
    {
        Assert.True(MatchCreationPolicy.CanCreate(Friendly(), userId, isAdmin: false, isOrganizer: false));
    }

    [Fact]
    public void Friendly_StrangerMayNotCreate()
    {
        Assert.False(MatchCreationPolicy.CanCreate(Friendly(), Stranger, isAdmin: false, isOrganizer: false));
    }

    [Fact]
    public void Friendly_OrganizerMayCreate()
    {
        // Товариський матч нікому не належить, тож організатор тут нічого не привласнює.
        Assert.True(MatchCreationPolicy.CanCreate(Friendly(), Stranger, isAdmin: false, isOrganizer: true));
    }

    // ---------- Турнірний матч ----------

    [Fact]
    public void Tournament_OwnerMayCreate()
    {
        Assert.True(MatchCreationPolicy.CanCreate(Tournament(), Owner, isAdmin: false, isOrganizer: true));
    }

    [Fact]
    public void Tournament_AnotherOrganizerMayNot()
    {
        // Роль Organizer дає право вести свій турнір, а не будь-чий.
        Assert.False(
            MatchCreationPolicy.CanCreate(Tournament(), OtherOrganizer, isAdmin: false, isOrganizer: true));
    }

    [Theory]
    [InlineData(HomeCaptain)]
    [InlineData(AwayCaptain)]
    public void Tournament_CaptainMayNotCreate(int userId)
    {
        // Інакше капітан дописував би собі матчі в чужу сітку.
        Assert.False(
            MatchCreationPolicy.CanCreate(Tournament(), userId, isAdmin: false, isOrganizer: false));
    }

    // ---------- Адміністратор ----------

    [Fact]
    public void Admin_MayCreateEitherKind()
    {
        Assert.True(MatchCreationPolicy.CanCreate(Friendly(), Admin, isAdmin: true, isOrganizer: false));
        Assert.True(MatchCreationPolicy.CanCreate(Tournament(), Admin, isAdmin: true, isOrganizer: false));
    }

    // ---------- Команда без капітана ----------

    [Fact]
    public void Friendly_TeamWithoutCaptainGrantsNobody()
    {
        // null-капітан не мусить збігатися з будь-ким, хто теж не має id.
        var context = new MatchCreationPolicy.Context(null, null, null, null);

        Assert.False(MatchCreationPolicy.CanCreate(context, Stranger, isAdmin: false, isOrganizer: false));
    }

    [Fact]
    public void IsFriendly_IsDecidedByTheAbsenceOfATournament()
    {
        Assert.True(MatchCreationPolicy.IsFriendly(Friendly()));
        Assert.False(MatchCreationPolicy.IsFriendly(Tournament()));
    }
}
