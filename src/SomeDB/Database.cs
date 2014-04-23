using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SomeDB
{
    // TODO dry up the code that writes to disk
    // TODO Read/Write Locking 4 thread safety

    public class Database
    {
        private readonly DirectoryInfo _dir;
        private readonly ISerializer _ser = new MyJsonSerializer();
        private readonly Dictionary<Type, Type[]> _subTypes = new Dictionary<Type, Type[]>();

        public Database(string storageDirectoryPath)
        {
            if (storageDirectoryPath == null) throw new ArgumentNullException("storageDirectoryPath");

            _dir = new DirectoryInfo(storageDirectoryPath);
            _dir.Create();
        }

        public T Load<T>(object id) where T : class
        {
            if (id == null) throw new ArgumentNullException("id");

            // find the first T with that id
            var type = typeof(T);
            var file = _dir.CreateSubdirectory(type.FullName).GetFile(id.ToString());

            return file.Exists
                ? (T)_ser.Deserialize(File.ReadAllText(file.FullName), type)
                : null;
        }

        public IQueryable<T> Query<T>() where T : class
        {
            return GetEnumerable<T>().AsQueryable();
        }

        public IEnumerable<T> GetEnumerable<T>() where T : class
        {
            return GetStoredItems<T>().Select(x => x.Value);
        }

        public IEnumerable<StoredItem<T>> GetStoredItems<T>() where T : class
        {
            var tDirs = GetTypeDirs<T>();

            var files = tDirs.SelectMany(dir => dir.Directory.EnumerateFiles(), (dir, file) => new
            {
                File = file,
                TypeDirectory = dir
            }).ToArray();


            return files.Select(x =>
            {
                var item = (T) _ser.Deserialize(File.ReadAllText(x.File.FullName), x.TypeDirectory.Type);

                return new StoredItem<T>(DeriveId(item), x.File, item);
            });

        }

        private IEnumerable<TypeDirectory> GetTypeDirs<T>()
        {
            var type = typeof(T);
            yield return new TypeDirectory(_dir, type);

            if (!_subTypes.ContainsKey(type))
                _subTypes[type] = type.GetAllSubTypes();

            foreach (var subType in _subTypes[type])
                yield return new TypeDirectory(_dir, subType);
        }

        private object DeriveId(object item)
        {
            return item.DeriveId(_ser);
        }

        public StoredItem<T> Save<T>(T item, object id = null) where T : class
        {
            if (item == null) throw new ArgumentNullException("item");
            id = id ?? DeriveId(item);

            // put file on disk in the T namespace named after id

            var type = item.GetType();
            var file = _dir.CreateSubdirectory(type.FullName).GetFile(id.ToString());
            File.WriteAllText(file.FullName, _ser.Serialize(item));

            return new StoredItem<T>(id, file, item);
        }

        public IEnumerable<StoredItem<T>> SaveAll<T>(IEnumerable<T> items) where T : class
        {
            if (items == null) throw new ArgumentNullException("items");
            foreach (var item in items)
                yield return Save(item);
        }

        public void DeleteById<T>(object id) where T : class
        {
            if (id == null) throw new ArgumentNullException("id");
            var type = typeof(T);
            var file = _dir.CreateSubdirectory(type.FullName).GetFile(id.ToString());
            file.Delete();
        }

        public void Delete<T>(T item) where T : class
        {
            if (item == null) throw new ArgumentNullException("item");

            var id = DeriveId(item);
            DeleteById<T>(id);
        }

        public void DeleteWhere<T>(Func<T, bool> predicate = null) where T : class
        {
            var items = GetStoredItems<T>()
                .Where(x => predicate == null || predicate(x.Value));

            foreach (var item in items)
                item.File.Delete();
        }

        public void DeleteAll<T>(IEnumerable<T> items) where T : class
        {
            if (items == null) throw new ArgumentNullException("items");

            foreach (var item in items)
                Delete(item);
        }

        public void Update<T>(Action<T> updateAction, Func<T, bool> predicate = null) where T : class
        {
            var items = GetStoredItems<T>();
            foreach (var item in items.Where(x => predicate == null || predicate(x.Value)))
            {
                updateAction(item.Value);
                Save(item);
            }
        }

        private void Save<T>(StoredItem<T> storedItem) where T : class
        {
            if (storedItem == null) throw new ArgumentNullException("storedItem");
            File.WriteAllText(storedItem.File.FullName, _ser.Serialize(storedItem.Value));
        }
    }
}
