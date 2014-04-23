using System;
using System.IO;

namespace SomeDB
{
    public class StoredItem<T> where T : class
    {
        public object Id { get; private set; }
        public FileInfo File { get; private set; }
        public T Value { get; private set; }
        
        // TODO start using this guy to cut down on how many times the object is serialized
        public string SerializedState { get; set; } 

        public StoredItem(object id, FileInfo file, T value)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (file == null) throw new ArgumentNullException("file");
            if (value == null) throw new ArgumentNullException("value");

            file.Refresh();

            Id = id;
            File = file;
            Value = value;
        }
    }
}