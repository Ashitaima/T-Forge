using TForge.DTOs;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Серверні id не мають ставати полями форми. Тест у дусі
/// TeamDtoSurfaceTests: він закріплює саму відсутність властивості, бо додати
/// її назад легко й непомітно.
/// </summary>
public class ChessTrainingDtoSurfaceTests
{
    [Theory]
    [InlineData("UploadedByUserId")]
    [InlineData("UploadedBy")]
    [InlineData("StoredPath")]
    public void UploadChessDatasetDto_HasNoServerDerivedProperty(string name)
    {
        Assert.Null(typeof(UploadChessDatasetDto).GetProperty(name));
    }

    [Theory]
    [InlineData("CreatedByUserId")]
    [InlineData("Status")]
    [InlineData("IsActive")]
    [InlineData("ModelPath")]
    public void CreateTrainingRunDto_HasNoServerDerivedProperty(string name)
    {
        Assert.Null(typeof(CreateTrainingRunDto).GetProperty(name));
    }

    [Fact]
    public void CreateTrainingRunDto_HasOnlyDatasetId()
    {
        var properties = typeof(CreateTrainingRunDto).GetProperties();
        Assert.Single(properties);
        Assert.Equal("DatasetId", properties[0].Name);
    }

    [Fact]
    public void TrainingRunDto_DoesNotLeakTheModelPath()
    {
        // Шлях на диску клієнтові ні до чого, і віддавати його — зайва
        // підказка про розкладку файлової системи сервера.
        Assert.Null(typeof(TrainingRunDto).GetProperty("ModelPath"));
    }
}
