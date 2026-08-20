using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class ProfileVisibilityTests
{
    private const int Owner = 5;
    private const int Stranger = 6;

    // ---- Хто бачить приховане ----

    [Fact]
    public void Owner_SeesOwnHiddenFields()
    {
        Assert.True(ProfileVisibility.CanSeeHidden(Owner, Owner, isAdmin: false));
    }

    [Fact]
    public void Stranger_DoesNot()
    {
        Assert.False(ProfileVisibility.CanSeeHidden(Owner, Stranger, isAdmin: false));
    }

    // Читання профілів відкрите, тож глядача може не бути зовсім.
    [Fact]
    public void Anonymous_DoesNot()
    {
        Assert.False(ProfileVisibility.CanSeeHidden(Owner, null, isAdmin: false));
    }

    // Адміністратор стоїть над правилами власності, як і скрізь у проєкті.
    [Fact]
    public void Admin_Does()
    {
        Assert.True(ProfileVisibility.CanSeeHidden(Owner, Stranger, isAdmin: true));
    }

    // ---- Застосування ----

    [Fact]
    public void HiddenText_BecomesEmpty()
    {
        Assert.Equal(string.Empty, ProfileVisibility.Apply("UA", isHidden: true, canSeeHidden: false));
    }

    [Fact]
    public void HiddenText_SurvivesForOwner()
    {
        Assert.Equal("UA", ProfileVisibility.Apply("UA", isHidden: true, canSeeHidden: true));
    }

    [Fact]
    public void VisibleText_IsUntouched()
    {
        Assert.Equal("UA", ProfileVisibility.Apply("UA", isHidden: false, canSeeHidden: false));
    }

    // Приховане поле не відрізняється від незаповненого — саме тому вік стає 0,
    // а не лишається справжнім.
    [Fact]
    public void HiddenAge_BecomesZero()
    {
        Assert.Equal(0, ProfileVisibility.Apply(25, isHidden: true, canSeeHidden: false));
    }

    [Fact]
    public void HiddenAge_SurvivesForOwner()
    {
        Assert.Equal(25, ProfileVisibility.Apply(25, isHidden: true, canSeeHidden: true));
    }

    [Fact]
    public void NullText_BecomesEmpty()
    {
        Assert.Equal(string.Empty, ProfileVisibility.Apply(null, isHidden: false, canSeeHidden: true));
    }
}
