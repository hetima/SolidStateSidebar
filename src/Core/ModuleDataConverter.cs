using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SSS.Core
{
    public class ModuleDataConverter : JsonConverter<Dictionary<string, IModuleData>>
    {
        private static readonly Dictionary<string, Type> KnownTypes = new()
        {
            { "CpuMonitor", typeof(Module.CpuMonitor.Data) },
            { "RamMonitor", typeof(Module.RamMonitor.Data) },
            { "GpuMonitor", typeof(Module.GpuMonitor.Data) },
            { "HdMonitor", typeof(Module.HdMonitor.Data) },
            { "NetworkMonitor", typeof(Module.NetworkMonitor.Data) },
            { "TimeMonitor", typeof(Module.TimeMonitor.Data) },
            { "WindowMonitor", typeof(Module.WindowMonitor.Data) },
            { "ClaudeMonitor", typeof(Module.ClaudeMonitor.Data) },
            { "CodexMonitor", typeof(Module.CodexMonitor.Data) }
        };

        public override Dictionary<string, IModuleData> ReadJson(JsonReader reader, Type objectType, Dictionary<string, IModuleData>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<string, IModuleData>();

            if (reader.TokenType == JsonToken.Null)
            {
                return result;
            }

            JObject obj = JObject.Load(reader);

            foreach (var property in obj.Properties())
            {
                if (KnownTypes.TryGetValue(property.Name, out Type? type))
                {
                    IModuleData? data = (IModuleData?)property.Value.ToObject(type, serializer);
                    if (data != null)
                    {
                        result[property.Name] = data;
                    }
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, Dictionary<string, IModuleData>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value, kvp.Value.GetType());
            }

            writer.WriteEndObject();
        }

        public static Dictionary<string, IModuleData> GetDefaults()
        {
            return new Dictionary<string, IModuleData>
            {
                { "CpuMonitor", Module.CpuMonitor.Data.Default },
                { "RamMonitor", Module.RamMonitor.Data.Default },
                { "GpuMonitor", Module.GpuMonitor.Data.Default },
                { "HdMonitor", Module.HdMonitor.Data.Default },
                { "NetworkMonitor", Module.NetworkMonitor.Data.Default },
                { "TimeMonitor", Module.TimeMonitor.Data.Default },
                { "WindowMonitor", Module.WindowMonitor.Data.Default },
                { "ClaudeMonitor", Module.ClaudeMonitor.Data.Default },
                { "CodexMonitor", Module.CodexMonitor.Data.Default }
            };
        }
    }
}
