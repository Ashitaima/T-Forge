using TForge.Services;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Хешування паролів. Клас не має залежностей, тож перевіряється напряму —
/// так само, як чисті калькулятори й політики.
/// </summary>
public class PasswordHasherTests
{
    private const string Password = "DevPassw0rd";

    /// <summary>
    /// SHA-256 від "DevPassw0rd" + "SALT_KEY" — рівно те, що лежить у базі,
    /// засіяній до переходу на BCrypt. Константа зафіксована навмисно:
    /// саме вона доводить, що старі акаунти ще можуть увійти.
    /// </summary>
    private const string LegacyHash = "HLTjfkxPTNT3x2rwCj5k0wA20LOLX12j8qrcrdMSTJk=";

    private static PasswordHasher Hasher() => new();

    // ---- BCrypt ----

    [Fact]
    public void Hash_ProducesBCryptFormat()
    {
        Assert.StartsWith("$2", Hasher().Hash(Password));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var hasher = Hasher();
        Assert.NotEqual(hasher.Hash(Password), hasher.Hash(Password));
    }

    [Fact]
    public void Verify_BCryptHashOfSamePassword_IsTrue()
    {
        var hasher = Hasher();
        Assert.True(hasher.Verify(Password, hasher.Hash(Password)));
    }

    [Fact]
    public void Verify_BCryptHashOfOtherPassword_IsFalse()
    {
        var hasher = Hasher();
        Assert.False(hasher.Verify("WrongPassw0rd", hasher.Hash(Password)));
    }

    // ---- Старі хеші ще працюють ----

    [Fact]
    public void Verify_LegacyHashOfSamePassword_IsTrue()
    {
        Assert.True(Hasher().Verify(Password, LegacyHash));
    }

    [Fact]
    public void Verify_LegacyHashOfOtherPassword_IsFalse()
    {
        Assert.False(Hasher().Verify("WrongPassw0rd", LegacyHash));
    }

    // ---- Що саме треба перехешувати ----

    [Fact]
    public void NeedsRehash_LegacyHash_IsTrue()
    {
        Assert.True(Hasher().NeedsRehash(LegacyHash));
    }

    [Fact]
    public void NeedsRehash_BCryptHash_IsFalse()
    {
        var hasher = Hasher();
        Assert.False(hasher.NeedsRehash(hasher.Hash(Password)));
    }

    // ---- Зіпсовані дані не повинні валити вхід ----

    [Theory]
    [InlineData("")]
    [InlineData("не хеш узагалі")]
    [InlineData("$2a$зіпсований")]
    public void Verify_MalformedHash_IsFalseAndDoesNotThrow(string hash)
    {
        Assert.False(Hasher().Verify(Password, hash));
    }
}
