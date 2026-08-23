using TForge.DTOs;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Статус вакансії задає сервер: щойно створена вакансія завжди Open —
/// те саме правило, за яким статус прибрали зі створення турніру.
/// Тримати це в тесті дешевше, ніж помітити згодом, що хтось додав поле
/// у форму й тим дозволив створити одразу «заповнену» вакансію.
/// </summary>
public class TeamOpeningDtoSurfaceTests
{
    [Theory]
    [InlineData(typeof(CreateTeamOpeningDto))]
    [InlineData(typeof(UpdateTeamOpeningDto))]
    public void WriteDtos_DoNotCarryStatus(Type dtoType) =>
        Assert.Null(dtoType.GetProperty("Status"));

    [Theory]
    [InlineData(typeof(CreateTeamOpeningDto))]
    [InlineData(typeof(UpdateTeamOpeningDto))]
    public void WriteDtos_DoNotCarryCreatedByUserId(Type dtoType) =>
        // Власника визначає сервер за токеном, як і всюди в проєкті.
        Assert.Null(dtoType.GetProperty("CreatedByUserId"));

    [Fact]
    public void ReadDto_ExposesStatus()
    {
        var property = typeof(TeamOpeningDto).GetProperty("Status");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }
}
