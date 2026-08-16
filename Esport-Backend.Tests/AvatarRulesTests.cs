using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class AvatarRulesTests
{
    private static byte[] Jpeg() => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };

    private static byte[] Png() => new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D
    };

    private static byte[] Webp()
    {
        var bytes = new byte[16];
        "RIFF"u8.CopyTo(bytes);
        "WEBP"u8.CopyTo(bytes.AsSpan(8));
        return bytes;
    }

    [Fact]
    public void DetectExtension_Jpeg_IsJpg() => Assert.Equal(".jpg", AvatarRules.DetectExtension(Jpeg()));

    [Fact]
    public void DetectExtension_Png_IsPng() => Assert.Equal(".png", AvatarRules.DetectExtension(Png()));

    [Fact]
    public void DetectExtension_Webp_IsWebp() => Assert.Equal(".webp", AvatarRules.DetectExtension(Webp()));

    // Файл із назвою .png, але текстовим вмістом, має бути відхилений:
    // саме тому дивимося на байти, а не на розширення чи Content-Type.
    [Fact]
    public void DetectExtension_TextPretendingToBePng_IsNull()
    {
        Assert.Null(AvatarRules.DetectExtension("not really an image"u8.ToArray()));
    }

    [Fact]
    public void DetectExtension_TooShort_IsNull()
    {
        Assert.Null(AvatarRules.DetectExtension(new byte[] { 0xFF }));
    }

    [Fact]
    public void DetectExtension_Empty_IsNull()
    {
        Assert.Null(AvatarRules.DetectExtension(Array.Empty<byte>()));
    }

    // RIFF-контейнер, але не WebP (наприклад, WAV) — не зображення.
    [Fact]
    public void DetectExtension_RiffButNotWebp_IsNull()
    {
        var bytes = new byte[16];
        "RIFF"u8.CopyTo(bytes);
        "WAVE"u8.CopyTo(bytes.AsSpan(8));
        Assert.Null(AvatarRules.DetectExtension(bytes));
    }

    [Fact]
    public void MaxBytes_IsTwoMegabytes()
    {
        Assert.Equal(2 * 1024 * 1024, AvatarRules.MaxBytes);
    }
}
