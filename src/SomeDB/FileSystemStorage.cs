using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SomeDB
{
    public class FileSystemStorage : IStorage
    {
        private readonly DirectoryInfo _dir;

        public FileSystemStorage(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            var di = new DirectoryInfo(rootPath);
            di.Create();
            _dir = di;
        }

        public FileSystemStorage()
            : this(GetDefaultRootPath())
        {
        }

        private static string GetDefaultRootPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appName = AppDomain.CurrentDomain.FriendlyName;
            return Path.Combine(appData, appName, "data");
        }

        public void Store(Type type, string id, string value)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            File.WriteAllText(file.FullName, value);
        }

        public string Retrieve(Type type, string id)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            var value = File.ReadAllText(file.FullName);
            return value;
        }

        public IEnumerable<string> RetrieveAll(Type type)
        {
            var dir = _dir.CreateSubdirectory(type.FullName);
            return dir.EnumerateFiles().Select(file => File.ReadAllText(file.FullName));
        }

        public void Remove(Type type, string id)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            if (file.Exists)
                file.Delete();
        }

        public void CopyTo(IStorage otherStorage)
        {
            var allTypes = AppDomain.CurrentDomain.GetTypes()
                .ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase);

            var q = from dir in _dir.GetDirectories()
                let type = allTypes[dir.Name].Single()
                where type != null
                from file in dir.EnumerateFiles()
                let id = file.Name
                select new
                {
                    type,
                    id,
                    value = File.ReadAllText(file.FullName)
                };

            foreach (var item in q)
                otherStorage.Store(item.type, item.id, item.value);
        }

        public void Purge()
        {
            foreach (var directoryInfo in _dir.GetDirectories())
                directoryInfo.Delete(true);
        }

        public void Purge(Type type)
        {
            foreach (var directoryInfo in _dir.EnumerateDirectories(type.FullName))
                directoryInfo.Delete(true);
        }
    }
}