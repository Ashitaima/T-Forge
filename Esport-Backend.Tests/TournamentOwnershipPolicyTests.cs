using TForge.Common;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Хто має право змінювати турнір. Та сама форма, що у FriendlyMatchPolicy:
/// чиста функція, яку видно наскрізь без бази.
/// </summary>
public class TournamentOwnershipPolicyTests
{
    private const int Owner = 10;
    private const int OtherOrganizer = 20;

    [Fact]
    public void CanManage_Owner_May()
    {
        Assert.True(TournamentOwnershipPolicy.CanManage(Owner, Owner, isAdmin: false));
    }

    // Головна дірка, яку закриває ця політика: організатор міг редагувати
    // будь-який турнір, зокрема й чужий.
    [Fact]
    public void CanManage_OtherOrganizer_MayNot()
    {
        Assert.False(TournamentOwnershipPolicy.CanManage(Owner, OtherOrganizer, isAdmin: false));
    }

    [Fact]
    public void CanManage_Admin_MayManageSomeoneElsesTournament()
    {
        Assert.True(TournamentOwnershipPolicy.CanManage(Owner, OtherOrganizer, isAdmin: true));
    }

    [Fact]
    public void CanManage_AdminWhoIsAlsoOwner_May()
    {
        Assert.True(TournamentOwnershipPolicy.CanManage(Owner, Owner, isAdmin: true));
    }

    // Турнір без організатора (0) не належить нікому, і випадковий нуль
    // у токені не повинен давати над ним влади.
    [Fact]
    public void CanManage_UnownedTournament_MayNotByDefault()
    {
        Assert.False(TournamentOwnershipPolicy.CanManage(0, OtherOrganizer, isAdmin: false));
    }
}
