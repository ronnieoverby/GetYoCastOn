namespace SomeDB
{
    public interface IIdFactory
    {
        void AssignNewId(IDocument doc);
    }
}