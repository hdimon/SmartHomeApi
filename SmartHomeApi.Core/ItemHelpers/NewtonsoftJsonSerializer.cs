using Common.Utils;
using Newtonsoft.Json;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core.ItemHelpers
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _options;

        public NewtonsoftJsonSerializer()
        {
            _options = new JsonSerializerSettings { Converters = { new NewtonsoftTimeSpanConverter() } };
        }

        public string Serialize(object value)
        {
            var content = JsonConvert.SerializeObject(value, Formatting.Indented, _options);

            return content;
        }

        public T Deserialize<T>(string value)
        {
            var obj = JsonConvert.DeserializeObject<T>(value, _options);
            return obj;
        }
    }
}