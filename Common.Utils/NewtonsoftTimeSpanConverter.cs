using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Utils
{
    /*public class NewtonsoftTimeSpanConverter : JsonConverter<TimeSpan>
    {
        /// <summary>
        /// Format: Days.Hours:Minutes:Seconds:Milliseconds
        /// </summary>
        public const string TimeSpanFormatString = @"d\.hh\:mm\:ss\:FFF";

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            var timespanFormatted = $"{value.ToString(TimeSpanFormatString)}";
            writer.WriteValue(timespanFormatted);
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            TimeSpan.TryParseExact((string)reader.Value, TimeSpanFormatString, null, out var parsedTimeSpan);
            return parsedTimeSpan;
        }
    }*/

    //https://stackoverflow.com/questions/57814077/how-can-i-choose-what-type-to-deserialize-at-runtime-based-on-the-structure-of-t
    public class NewtonsoftTimeSpanConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;

                case JTokenType.String:
                    var value = (string)token;

                    if (TimeSpan.TryParse(value, out var timeSpan))
                        return timeSpan;

                    return token.DefaultToObject(objectType, serializer);

                default:
                    return token.DefaultToObject(objectType, serializer);
            }
        }

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(object);
    }

    public static class JsonExtensions
    {
        public static object DefaultToObject(this JToken token, Type type, JsonSerializer serializer = null)
        {
            var oldParent = token.Parent;

            var dtoToken = new JObject(new JProperty(nameof(DefaultSerializationDTO<object>.Value), token));
            var dtoType = typeof(DefaultSerializationDTO<>).MakeGenericType(type);
            var dto = (IHasValue)(serializer ?? JsonSerializer.CreateDefault()).Deserialize(dtoToken.CreateReader(), dtoType);

            if (oldParent == null)
                token.RemoveFromLowestPossibleParent();

            return dto == null ? null : dto.GetValue();
        }

        public static JToken RemoveFromLowestPossibleParent(this JToken node)
        {
            if (node == null)
                return null;
            // If the parent is a JProperty, remove that instead of the token itself.
            var contained = node.Parent is JProperty ? node.Parent : node;
            contained.Remove();
            // Also detach the node from its immediate containing property -- Remove() does not do this even though it seems like it should
            if (contained is JProperty)
                ((JProperty)node.Parent).Value = null;
            return node;
        }

        interface IHasValue
        {
            object GetValue();
        }

        [JsonObject(NamingStrategyType = typeof(Newtonsoft.Json.Serialization.DefaultNamingStrategy), IsReference = false)]
        class DefaultSerializationDTO<T> : IHasValue
        {
            public DefaultSerializationDTO(T value)
            {
                this.Value = value;
            }

            public DefaultSerializationDTO()
            {
            }

            [JsonConverter(typeof(NoConverter)), JsonProperty(ReferenceLoopHandling = ReferenceLoopHandling.Serialize)]
            public T Value { get; set; }

            public object GetValue() => Value;
        }
    }

    public class NoConverter : JsonConverter
    {
        // NoConverter taken from this answer https://stackoverflow.com/a/39739105/3744182
        // To https://stackoverflow.com/questions/39738714/selectively-use-default-json-converter
        // By https://stackoverflow.com/users/3744182/dbc
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException(); /* This converter should only be applied via attributes */
        }

        public override bool CanRead => false;
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}