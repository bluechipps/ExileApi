using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Nodes;

namespace Shared.Nodes
{
    public class ToggleNodeConverter : CustomCreationConverter<ToggleNode>
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override ToggleNode Create(Type objectType) => new ToggleNode(false);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            new ToggleNode(serializer.Deserialize<bool>(reader));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            serializer.Serialize(writer, value is ToggleNode toggleNode && toggleNode.Value);
    }
}