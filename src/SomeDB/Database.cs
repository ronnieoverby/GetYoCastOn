using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SomeDB
{
    // TODO abstract serialization?
    // TODO use a serializer that wont barf when property types are changed
    // TODO dry up the code that writes to disk
    // TODO polymorphic queries :)

    public class Database
    {
        private readonly DirectoryInfo _dir;

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
            var ser = new XmlSerializer(type);

            var file = _dir.CreateSubdirectory(type.FullName).GetFile(id.ToString());

            if (!file.Exists)
                return null;

            using (var stream = file.OpenRead())
                return (T)ser.Deserialize(stream);
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
            var type = typeof(T);
            var ser = new XmlSerializer(type);

            var tDir = _dir.CreateSubdirectory(type.FullName);

            return tDir.EnumerateFiles().Select(f =>
            {
                using (var stream = f.OpenRead())
                {
                    var item = (T)ser.Deserialize(stream);
                    return new StoredItem<T>(item.DeriveId(), f, item);
                }
            });
        }

        public StoredItem<T> Save<T>(T item) where T : class
        {
            if (item == null) throw new ArgumentNullException("item");
            var id = item.DeriveId();

            // put file on disk in the T namespace named after id

            var type = item.GetType();
            var ser = new XmlSerializer(type);
            var file = _dir.CreateSubdirectory(type.FullName).GetFile(id.ToString());


            using (var stream = File.Open(file.FullName, FileMode.Create))
                ser.Serialize(stream, item);

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

            var id = item.DeriveId();
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

            var type = typeof(T);
            var ser = new XmlSerializer(type);
            using (var stream = File.Open(storedItem.File.FullName, FileMode.Create))
                ser.Serialize(stream, storedItem.Value);
        }
    }
}
