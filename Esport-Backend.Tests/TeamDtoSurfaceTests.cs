using TForge.DTOs;

namespace TForge.Tests;

/// <summary>
/// Шлях до логотипа задає сервер, а не клієнт. Тримати це правило в тесті
/// дешевше, ніж помітити згодом, що хтось додав LogoPath у форму й тим
/// відкрив запис довільного шляху в колонку.
/// </summary>
public class TeamDtoSurfaceTests
{
    [Theory]
    [InlineData(typeof(CreateTeamDto))]
    [InlineData(typeof(UpdateTeamDto))]
    public void WriteDtos_DoNotCarryLogoPath(Type dtoType)
    {
        Assert.Null(dtoType.GetProperty("LogoPath"));
    }

    [Theory]
    [InlineData(typeof(TeamDto))]
    [InlineData(typeof(TeamSummaryDto))]
    [InlineData(typeof(TeamRowDto))]
    public void ReadDtos_ExposeLogoPath(Type dtoType)
    {
        var property = dtoType.GetProperty("LogoPath");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }
}
