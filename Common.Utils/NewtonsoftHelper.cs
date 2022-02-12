using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Linq;

namespace Common.Utils
{
    public class NewtonsoftHelper
    {
        public static object ParseJsonAsExpando(string json)
        {
            var jToken = JToken.Parse(json);

            return ParseJTokenAsExpando(jToken);
        }

        public static object ParseJTokenAsExpando(JToken token)
        {
            return token switch
            {
                null => null,
                JObject jObj => jObj.ToObject<ExpandoObject>(),
                JValue jVal => jVal.ToObject<object>(),
                JArray jArr => ParseJArrayAsExpando(jArr),
                _ => null
            };
        }

        private static object ParseJArrayAsExpando(JArray token)
        {
            var result = new List<object>();

            var list = token.ToObject<List<object>>();

            if (list == null) throw new ArgumentException("Can't parse JSON array as List<object>");

            foreach (var item in list)
            {
                if (item is JToken nestedToken) result.Add(ParseJTokenAsExpando(nestedToken));
                else result.Add(item);
            }

            return result;
        }
    }
}