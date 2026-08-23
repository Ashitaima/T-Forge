using System.Text.RegularExpressions;
using TForge.Common;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

public class AccountAnonymizerTests
{
    // ---- Значення виводяться з id, бо колонки унікальні ----

    [Fact]
    public void UsernameFor_IsDerivedFromId() =>
        Assert.Equal("deleted_user_7", AccountAnonymizer.UsernameFor(7));

    [Fact]
    public void EmailFor_IsDerivedFromId() =>
        Assert.Equal("deleted_7@deleted.invalid", AccountAnonymizer.EmailFor(7));

    [Fact]
    public void NicknameFor_IsDerivedFromId() =>
        Assert.Equal("deleted_player_7", AccountAnonymizer.NicknameFor(7));

    [Theory]
    [InlineData(1, 2)]
    [InlineData(7, 70)]
    [InlineData(100, 1000)]
    public void DifferentIds_NeverCollide(int first, int second)
    {
        // Username, Email і Nickname несуть унікальні індекси: два видалені
        // акаунти з однаковим значенням не збереглися б узагалі.
        Assert.NotEqual(AccountAnonymizer.UsernameFor(first), AccountAnonymizer.UsernameFor(second));
        Assert.NotEqual(AccountAnonymizer.EmailFor(first), AccountAnonymizer.EmailFor(second));
        Assert.NotEqual(AccountAnonymizer.NicknameFor(first), AccountAnonymizer.NicknameFor(second));
    }

    // ---- Підставлені значення мусять проходити ті самі правила ----

    [Theory]
    [InlineData(1)]
    [InlineData(99999)]
    public void NicknameFor_SatisfiesPlayerNicknameRules(int playerId)
    {
        // Нікнейм пише сервіс, але правила з PlayerRules нікуди не діваються:
        // збережений рядок мусить лишатися редагованим через звичайну форму.
        var nickname = AccountAnonymizer.NicknameFor(playerId);

        Assert.InRange(nickname.Length, PlayerRules.NicknameMinLength, PlayerRules.NicknameMaxLength);
        Assert.Matches("^[a-zA-Z0-9_]+$", nickname);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99999)]
    public void EmailFor_IsAValidAddressShape(int userId) =>
        Assert.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", AccountAnonymizer.EmailFor(userId));

    [Fact]
    public void EmailFor_UsesAReservedDomainThatCanNeverReceiveMail() =>
        // .invalid зарезервовано RFC 2606 — лист туди не піде нікому реальному.
        Assert.EndsWith(".invalid", AccountAnonymizer.EmailFor(3));

    // ---- Розпізнавання вже знеособленого рядка ----

    [Fact]
    public void IsAnonymized_AnonymizedUsername_IsTrue() =>
        Assert.True(AccountAnonymizer.IsAnonymized(AccountAnonymizer.UsernameFor(4)));

    [Theory]
    [InlineData("Kenzi")]
    [InlineData("deleted")]
    [InlineData("")]
    [InlineData(null)]
    public void IsAnonymized_RealUsername_IsFalse(string? username) =>
        Assert.False(AccountAnonymizer.IsAnonymized(username));

    // ---- Пароль після знеособлення не підходить жоден ----

    [Fact]
    public void UnusablePasswordHash_IsEmpty() =>
        // PasswordHasher.Verify на порожньому рядку повертає false, не кидаючи, —
        // тож знеособлений акаунт не пускає нікого й без окремої перевірки.
        Assert.Empty(AccountAnonymizer.UnusablePasswordHash);

    [Fact]
    public void DisplayNickname_IsHumanReadable() =>
        // Те, що бачать у списках і ростерах замість нікнейма.
        Assert.False(string.IsNullOrWhiteSpace(AccountAnonymizer.DeletedDisplayName));
}
