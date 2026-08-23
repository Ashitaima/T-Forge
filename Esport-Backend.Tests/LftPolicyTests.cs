using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class LftPolicyTests
{
    private const int Captain = 10;
    private const int Stranger = 20;

    // ---- Вакансію публікує капітан ----

    [Fact]
    public void CanManageOpening_Captain_May() =>
        Assert.True(LftPolicy.CanManageOpening(Captain, Captain, isAdmin: false));

    [Fact]
    public void CanManageOpening_Stranger_MayNot() =>
        Assert.False(LftPolicy.CanManageOpening(Captain, Stranger, isAdmin: false));

    [Fact]
    public void CanManageOpening_Admin_May() =>
        // Адміністратор лишається над правилом власності, як і всюди.
        Assert.True(LftPolicy.CanManageOpening(Captain, Stranger, isAdmin: true));

    // ---- Змах по власній вакансії ----

    [Fact]
    public void CanSwipeAsPlayer_Stranger_May() =>
        Assert.True(LftPolicy.CanSwipeAsPlayer(Captain, Stranger));

    [Fact]
    public void CanSwipeAsPlayer_CaptainOfThatVeryTeam_MayNot() =>
        // Інакше капітан дав би згоду за обидві сторони, і в складі опинився
        // б гравець, який на це не погоджувався. Те саме правило, що в
        // MatchChallengePolicy.CanRespond для ініціатора.
        Assert.False(LftPolicy.CanSwipeAsPlayer(Captain, Captain));
}
