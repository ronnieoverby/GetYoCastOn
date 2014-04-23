using System;
using Newtonsoft.Json;

namespace SomeDB
{
    internal class MyJsonSerializer : ISerializer
    {
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public object Deserialize(string s,Type type)
        {
            return JsonConvert.DeserializeObject(s, type);
        }
    }
}