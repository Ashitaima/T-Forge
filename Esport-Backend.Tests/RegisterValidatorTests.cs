using TForge.Common;
using TForge.DTOs;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private static RegisterDto Valid(string role, string nickname = "") => new()
    {
        Username = "newcomer",
        Email = "newcomer@example.com",
        Password = "DevPassw0rd",
        FirstName = "Тарас",
        LastName = "Шевченко",
        Role = role,
        Nickname = nickname
    };

    // ---- Роль ----

    [Fact]
    public void Role_Player_IsAccepted()
    {
        Assert.True(_validator.Validate(Valid(UserRoles.Player, "s1mple")).IsValid);
    }

    [Fact]
    public void Role_Organizer_IsAccepted()
    {
        Assert.True(_validator.Validate(Valid(UserRoles.Organizer)).IsValid);
    }

    // Публічна реєстрація не повинна видавати права адміністратора.
    [Fact]
    public void Role_Admin_IsRejected()
    {
        var result = _validator.Validate(Valid(UserRoles.Admin));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterDto.Role));
    }

    [Fact]
    public void Role_LegacyUser_IsRejected()
    {
        Assert.False(_validator.Validate(Valid(UserRoles.LegacyUser)).IsValid);
    }

    // ---- Нікнейм ----

    [Fact]
    public void Nickname_MissingForPlayer_IsRejected()
    {
        var result = _validator.Validate(Valid(UserRoles.Player, ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterDto.Nickname));
    }

    [Fact]
    public void Nickname_TooShortForPlayer_IsRejected()
    {
        Assert.False(_validator.Validate(Valid(UserRoles.Player, "ab")).IsValid);
    }

    [Fact]
    public void Nickname_TooLongForPlayer_IsRejected()
    {
        Assert.False(_validator.Validate(Valid(UserRoles.Player, new string('a', 31))).IsValid);
    }

    // Організатору профіль гравця не створюється, тож нікнейм йому не потрібен.
    [Fact]
    public void Nickname_MissingForOrganizer_IsAccepted()
    {
        Assert.True(_validator.Validate(Valid(UserRoles.Organizer, "")).IsValid);
    }
}
