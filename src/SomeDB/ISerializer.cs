namespace SomeDB
{
    public interface ISerializer
    {
        string Serialize(object value);
        T Deserialize<T>(string s);
    }
}