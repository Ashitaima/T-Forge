using TForge.Common;

namespace TForge.Tests;

/// <summary>
/// Формати ігрових тегів. Неправильний тег нічим себе не виказує доти, доки
/// за ним не спробують знайти гравця, — тож межі кожного формату варто
/// зафіксувати тут, а не з'ясовувати їх на живому профілі.
/// </summary>
public class GameIdFormatsTests
{
    // ---------- Riot ID ----------

    [Theory]
    [InlineData("Shroud#EUW")]
    [InlineData("abc#12")]                 // найкоротше ім'я і найкоротший тег
    [InlineData("SixteenCharsName#12345")] // найдовше ім'я і найдовший тег
    [InlineData("Player One#NA1")]         // пробіл усередині імені Riot дозволяє
    [InlineData(" Shroud#EUW ")]           // вставили з буфера — краї обрізаємо
    public void IsRiotId_AcceptsWellFormed(string value)
    {
        Assert.True(GameIdFormats.IsRiotId(value));
    }

    [Theory]
    [InlineData("Shroud")]                  // без тега
    [InlineData("Shroud#")]                 // порожній тег
    [InlineData("#EUW")]                    // без імені
    [InlineData("ab#EUW")]                  // ім'я коротше за три символи
    [InlineData("SeventeenCharsNam#EU")]    // ім'я довше за шістнадцять
    [InlineData("Shroud#E")]                // тег коротший за два символи
    [InlineData("Shroud#TOOLONG")]          // тег довший за п'ять
    [InlineData("Shroud#EU_W")]             // у тезі лише літери й цифри
    [InlineData("Sh#roud#EUW")]             // друга решітка
    [InlineData("Shroud #EUW")]             // пробіл перед решіткою
    public void IsRiotId_RejectsMalformed(string value)
    {
        Assert.False(GameIdFormats.IsRiotId(value));
    }

    // ---------- SteamID64 ----------

    [Theory]
    [InlineData("76561197960265728")] // база — найменший можливий SteamID64
    [InlineData("76561198000000000")]
    public void IsSteamId64_AcceptsSeventeenDigitsFromBase(string value)
    {
        Assert.True(GameIdFormats.IsSteamId64(value));
    }

    [Theory]
    [InlineData("7656119796026572")]   // шістнадцять цифр
    [InlineData("765611979602657289")] // вісімнадцять
    [InlineData("12345678901234567")]  // правильна довжина, чужий префікс
    [InlineData("STEAM_0:0:12345")]    // формат SteamID2, а не 64
    [InlineData("7656119796026572a")]
    public void IsSteamId64_RejectsAnythingElse(string value)
    {
        Assert.False(GameIdFormats.IsSteamId64(value));
    }

    // ---------- BattleTag ----------

    [Theory]
    [InlineData("Player#1234")]
    [InlineData("Abc#1234")]           // найкоротше ім'я
    [InlineData("TwelveCharss#12345")] // найдовше ім'я, п'ятизначний номер
    public void IsBattleTag_AcceptsWellFormed(string value)
    {
        Assert.True(GameIdFormats.IsBattleTag(value));
    }

    [Theory]
    [InlineData("Player#123")]          // номер коротший за чотири цифри
    [InlineData("Player#123456")]       // довший за п'ять
    [InlineData("1Player#1234")]        // починається з цифри
    [InlineData("Ab#1234")]             // ім'я коротше за три символи
    [InlineData("ThirteenChars#1234")]  // ім'я довше за дванадцять
    [InlineData("Play er#1234")]        // пробіл усередині
    [InlineData("Player#12a4")]
    [InlineData("Player")]
    public void IsBattleTag_RejectsMalformed(string value)
    {
        Assert.False(GameIdFormats.IsBattleTag(value));
    }

    // ---------- Порожнє значення ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EveryFormat_TreatsEmptyAsValid(string? value)
    {
        // Тег необов'язковий: гравець без акаунта в Riot не мусить його вигадувати.
        Assert.True(GameIdFormats.IsRiotId(value));
        Assert.True(GameIdFormats.IsSteamId64(value));
        Assert.True(GameIdFormats.IsBattleTag(value));
    }

    // ---------- Нормалізація ----------

    [Theory]
    [InlineData("  Shroud#EUW  ", "Shroud#EUW")]
    [InlineData("Shroud#EUW", "Shroud#EUW")]
    public void Normalize_TrimsWhatGetsStored(string input, string expected)
    {
        // Інакше в колонку лягає рядок, який сам би не пройшов перевірку.
        Assert.Equal(expected, GameIdFormats.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_TurnsEmptyIntoNull(string? input)
    {
        Assert.Null(GameIdFormats.Normalize(input));
    }

    // ---------- Посилання на Steam ----------

    [Fact]
    public void SteamProfileUrl_BuildsCanonicalAddress()
    {
        Assert.Equal(
            "https://steamcommunity.com/profiles/76561197960265728",
            GameIdFormats.SteamProfileUrl("76561197960265728"));
    }

    [Fact]
    public void SteamProfileUrl_TrimsBeforeBuilding()
    {
        Assert.Equal(
            "https://steamcommunity.com/profiles/76561197960265728",
            GameIdFormats.SteamProfileUrl("  76561197960265728  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-steam-id")]
    public void SteamProfileUrl_IsNullWhenThereIsNothingToLinkTo(string? value)
    {
        // Краще не показати посилання, ніж повести за адресою, якої немає.
        Assert.Null(GameIdFormats.SteamProfileUrl(value));
    }
}
