using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class UserRolesTests
{
    [Theory]
    [InlineData("Player")]
    [InlineData("Organizer")]
    [InlineData("Admin")]
    [InlineData("User")]
    public void IsValid_KnownRole_IsTrue(string role)
    {
        Assert.True(UserRoles.IsValid(role));
    }

    [Theory]
    [InlineData("Moderator")]
    [InlineData("player")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownRole_IsFalse(string? role)
    {
        Assert.False(UserRoles.IsValid(role));
    }

    // Публічна реєстрація не повинна видавати адміністративні права.
    [Theory]
    [InlineData("Player")]
    [InlineData("Organizer")]
    public void IsSelfService_AllowedRole_IsTrue(string role)
    {
        Assert.True(UserRoles.IsSelfService(role));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData(null)]
    public void IsSelfService_DisallowedRole_IsFalse(string? role)
    {
        Assert.False(UserRoles.IsSelfService(role));
    }

    [Fact]
    public void SelfService_IsASubsetOfAll()
    {
        Assert.All(UserRoles.SelfService, role => Assert.Contains(role, UserRoles.All));
    }
}
