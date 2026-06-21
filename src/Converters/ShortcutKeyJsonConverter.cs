using System;
using System.Windows.Input;
using Newtonsoft.Json;
using SSS.Windows;

namespace SSS.Converters
{
    /// <summary>
    /// ShortcutKey を JSON 文字列（例: "Ctrl+Shift+A"）としてシリアライズするコンバーター
    /// </summary>
    public class ShortcutKeyJsonConverter : JsonConverter<ShortcutKey>
    {
        public override ShortcutKey ReadJson(JsonReader reader, Type objectType, ShortcutKey? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var s = reader.Value as string;
            if (string.IsNullOrWhiteSpace(s))
                return new ShortcutKey(Key.None);

            ShortcutKey.TryParse(s, out var result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, ShortcutKey? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString() ?? string.Empty);
        }
    }
}
