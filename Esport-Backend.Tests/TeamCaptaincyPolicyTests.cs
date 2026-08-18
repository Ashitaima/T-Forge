using TForge.Common;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Хто може передати капітанство. Та сама форма, що у TournamentOwnershipPolicy.
/// </summary>
public class TeamCaptaincyPolicyTests
{
    private const int Captain = 10;
    private const int SomeoneElse = 20;

    [Fact]
    public void CanTransfer_Captain_May()
    {
        Assert.True(TeamCaptaincyPolicy.CanTransfer(Captain, Captain, isAdmin: false));
    }

    // Гравець команди — теж «хтось інший»: капітанство передає чинний капітан,
    // а не той, хто його хоче.
    [Fact]
    public void CanTransfer_AnotherUser_MayNot()
    {
        Assert.False(TeamCaptaincyPolicy.CanTransfer(Captain, SomeoneElse, isAdmin: false));
    }

    [Fact]
    public void CanTransfer_Admin_MayForSomeoneElsesTeam()
    {
        Assert.True(TeamCaptaincyPolicy.CanTransfer(Captain, SomeoneElse, isAdmin: true));
    }

    // Команда без капітана (0) не належить нікому, і випадковий нуль
    // у токені не повинен давати над нею влади.
    [Fact]
    public void CanTransfer_TeamWithoutCaptain_MayNotByDefault()
    {
        Assert.False(TeamCaptaincyPolicy.CanTransfer(0, SomeoneElse, isAdmin: false));
    }

    [Fact]
    public void CanTransfer_TeamWithoutCaptain_AdminStillMay()
    {
        Assert.True(TeamCaptaincyPolicy.CanTransfer(0, SomeoneElse, isAdmin: true));
    }
}
