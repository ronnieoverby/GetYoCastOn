using System;

namespace SomeDB
{
    public interface ISerializer
    {
        string Serialize(object value);
        object Deserialize(string s, Type type);
    }
}