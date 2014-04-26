using System;
using System.Collections.Generic;
using System.Linq;

namespace SomeDB
{
    public class MemoryStorage : IStorage
    {
        private readonly Dictionary<Type, Dictionary<string, string>> _types =
            new Dictionary<Type, Dictionary<string, string>>();

        public void Store(Type type, string id, string value)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");
            if (value == null) throw new ArgumentNullException("value");

            var dict = _types.ContainsKey(type)
                ? _types[type]
                : (_types[type] = new Dictionary<string, string>());

            dict[id] = value;
        }

        public string Retrieve(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            return !_types.ContainsKey(type)
                ? null
                : (!_types[type].ContainsKey(id)
                    ? null
                    : _types[type][id]);
        }

        public IEnumerable<string> RetrieveAll(Type type)
        {

            if (!_types.ContainsKey(type))
                return new String[0];

            return _types[type].Values.ToArray();
        }

        public void Remove(Type type, string id)
        {
            if (!_types.ContainsKey(type))
                return;

            _types[type].Remove(id);
        }

        public void CopyTo(IStorage otherStorage)
        {
            var q = from typeItem in _types
                from item in typeItem.Value
                select new
                {
                    type = typeItem.Key,
                    id = item.Key,
                    value = item.Value
                };

            foreach (var item in q)
                otherStorage.Store(item.type, item.id, item.value);
        }
    }
}