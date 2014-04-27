using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Collections.Generic;

namespace SomeDB.Storage
{
    public class EsentStorage : IPersistentStorage, IDisposable
    {
        private readonly ISerializer _serializer;
        readonly private PersistentDictionary<string, string> _data;

        public EsentStorage(string directory = null, ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(directory))
                directory = this.GetDefaultDirectory();

            if (serializer == null)
                serializer = new JsonSerializer();

            _serializer = serializer;
            _data = new PersistentDictionary<string, string>(SanitizeDirectory(directory));
        }

        private static string SanitizeDirectory(string directory)
        {
            return string.Concat(directory.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        }

        public IEnumerable<IDocument> Store(IEnumerable<IDocument> documents)
        {
            foreach (var document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.Id))
                {
                    throw null;
                }

                var key = GetKey(document.GetType(), document.Id);
                var value = _serializer.Serialize(document);
                _data[key] = value;
                yield return document;
            }

            _data.Flush();
        }

        private const char Hash = '#';
        private static string GetKeyPrefix(Type type)
        {
            return type.FullName + Hash;
        }

        private static string GetKey(Type type, string id)
        {
            return GetKeyPrefix(type) + id;
        }

        public IDocument Retrieve(Type type, string id)
        {
            var key = GetKey(type, id);
            if (!_data.ContainsKey(key)) return null;
            var value = _data[key];
            var doc = _serializer.Deserialize(value, type);
            return (IDocument)doc;
        }

        public IEnumerable<IDocument> RetrieveAll(Type type)
        {
            return _data.Where(x => x.Key.StartsWith(GetKeyPrefix(type)))
                .Select(x => _serializer.Deserialize(x.Value, type)).Cast<IDocument>();
        }

        public IEnumerable<IDocument> RetrieveAll()
        {
            var docTypes = AppDomain.CurrentDomain.GetTypes()
                .Where(x => typeof (IDocument).IsAssignableFrom(x))
                .ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase);

            return from item in _data
                let typeName = item.Key.Substring(0, item.Key.IndexOf(Hash))
                let type = docTypes[typeName].Single(x => typeof (IDocument).IsAssignableFrom(x))
                select (IDocument) _serializer.Deserialize(item.Value, type);
        }

        public void Remove(Type type, string id)
        {
            var key = GetKey(type, id);

            if (_data.ContainsKey(key))
                _data.Remove(key);
        }



        public void Dispose()
        {
            if (_data != null)
                _data.Dispose();
        }
    }
}