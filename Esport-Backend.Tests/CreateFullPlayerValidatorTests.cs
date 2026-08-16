using TForge.DTOs;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

public class CreateFullPlayerValidatorTests
{
    private readonly CreateFullPlayerValidator _validator = new();

    private static CreateFullPlayerDto Valid() => new()
    {
        Username = "newplayer",
        Email = "newplayer@example.com",
        Password = "DevPassw0rd",
        FirstName = "Іван",
        LastName = "Франко",
        Nickname = "ivan",
        Position = "Support",
        Country = "Україна",
        Age = 20
    };

    [Fact]
    public void FullyPopulated_IsValid()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Username_TooShort_IsRejected()
    {
        var dto = Valid();
        dto.Username = "ab";
        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Username_WithSpaces_IsRejected()
    {
        var dto = Valid();
        dto.Username = "new player";
        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Email_Malformed_IsRejected()
    {
        var dto = Valid();
        dto.Email = "not-an-email";
        Assert.False(_validator.Validate(dto).IsValid);
    }

    // Той самий поріг складності, що й у публічній реєстрації.
    [Fact]
    public void Password_WithoutDigit_IsRejected()
    {
        var dto = Valid();
        dto.Password = "NoDigitsHere";
        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Nickname_Missing_IsRejected()
    {
        var dto = Valid();
        dto.Nickname = "";
        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Age_BelowTwelve_IsRejected()
    {
        var dto = Valid();
        dto.Age = 11;
        Assert.False(_validator.Validate(dto).IsValid);
    }
}
