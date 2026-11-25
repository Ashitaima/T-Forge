using TForge.DTOs;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

public class ChessTrainingValidatorTests
{
    private readonly UploadChessDatasetDtoValidator _upload = new();
    private readonly CreateTrainingRunDtoValidator _run = new();

    [Fact]
    public void Upload_ValidName_Passes()
    {
        Assert.True(_upload.Validate(new UploadChessDatasetDto { Name = "Lichess 2026-01" }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Upload_ShortOrEmptyName_Fails(string name)
    {
        Assert.False(_upload.Validate(new UploadChessDatasetDto { Name = name }).IsValid);
    }

    [Fact]
    public void Upload_TooLongName_Fails()
    {
        Assert.False(_upload.Validate(new UploadChessDatasetDto { Name = new string('x', 101) }).IsValid);
    }

    [Fact]
    public void Run_PositiveDatasetId_Passes()
    {
        Assert.True(_run.Validate(new CreateTrainingRunDto { DatasetId = 1 }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Run_NonPositiveDatasetId_Fails(int id)
    {
        Assert.False(_run.Validate(new CreateTrainingRunDto { DatasetId = id }).IsValid);
    }
}
