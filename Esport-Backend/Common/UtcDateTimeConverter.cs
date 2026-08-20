using System.Text.Json;
using System.Text.Json.Serialization;

namespace TForge.Common
{
    /// <summary>
    /// Приводить дати з тіла запиту до UTC.
    ///
    /// Колонки з датами — це PostgreSQL `timestamp with time zone`, і Npgsql
    /// відмовляється писати в них DateTime із Kind=Unspecified. А приходить
    /// саме такий: components/ui/DateTimePicker.tsx надсилає «2026-09-01T18:00»
    /// без зсуву, бо це той самий формат, що й у нативного input, і
    /// System.Text.Json читає його як Unspecified. Через це будь-яке створення
    /// матчу, дуелі, виклику чи турніру падало з 500.
    ///
    /// Дату без зсуву вважаємо місцевою — саме її користувач бачив у полі, —
    /// і переводимо в UTC. Тоді значення повертається на клієнт тим самим:
    /// toLocaleString малює ті ж 18:00. Тлумачити її як UTC не можна — тоді
    /// збережене й показане розходилися б на величину зсуву.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            ToUtc(reader.GetDateTime());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToUtc(value));

        /// <summary>Спільне правило — щоб обидва конвертери не розійшлися.</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }

    /// <summary>Те саме для необов'язкових дат — null лишається null.</summary>
    public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? null
                : UtcDateTimeConverter.ToUtc(reader.GetDateTime());

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(UtcDateTimeConverter.ToUtc(value.Value));
        }
    }
}
