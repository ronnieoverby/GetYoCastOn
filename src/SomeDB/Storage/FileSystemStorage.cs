using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SomeDB.Storage
{
    public class FileSystemStorage : IStorage
    {
        private readonly DirectoryInfo _dir;

        private readonly ReaderWriterLockSlim _masterLock = new ReaderWriterLockSlim();

        private const int FileLockCap = 100000;
        private readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _fileLocks =
            new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);

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

            Write(file.FullName, value);
        }

        public string Retrieve(Type type, string id)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            var value = Read(file.FullName);
            return value;
        }

        public IEnumerable<string> RetrieveAll(Type type)
        {
            var dir = _dir.CreateSubdirectory(type.FullName);
            return
                dir.EnumerateFiles().Select(file => Read(file.FullName));
        }

        private string Read(string fullName)
        {
            return WithLock(fullName, lck =>
            {
                lck.EnterReadLock();
                try
                {
                    return !File.Exists(fullName) ? null : File.ReadAllText(fullName);
                }
                finally
                {
                    lck.ExitReadLock();
                }
            });
        }

        private void Exclusive(Action action)
        {
            _masterLock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                _masterLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Keeps file lock count low.
        /// </summary>
        private T WithLock<T>(string key, Func<ReaderWriterLockSlim, T> func)
        {
            _masterLock.EnterReadLock();
            try
            {
                if (!(_fileLocks.Count > FileLockCap))
                    return func(_fileLocks.GetOrAdd(key, new ReaderWriterLockSlim()));
            }
            finally
            {
                _masterLock.ExitReadLock();
            }

            _masterLock.EnterWriteLock();
            try
            {
                if (_fileLocks.Count > FileLockCap)
                    _fileLocks.Clear();

                return func(_fileLocks.GetOrAdd(key, new ReaderWriterLockSlim()));
            }
            finally
            {
                _masterLock.ExitWriteLock();
            }
        }

        private void Write(string fullName, string value)
        {
            WithLock(fullName, lck =>
            {
                lck.EnterWriteLock();
                try
                {
                    File.WriteAllText(fullName, value);
                    return 0;
                }
                finally
                {
                    lck.ExitWriteLock();
                }
            });
        }

        private void Delete(string fullName)
        {
            WithLock(fullName, lck =>
            {
                lck.EnterWriteLock();
                try
                {
                    if (File.Exists(fullName))
                        File.Delete(fullName);

                    return 0;
                }
                finally
                {
                    lck.ExitWriteLock();
                }
            });
        }

        public void Remove(Type type, string id)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            Delete(file.FullName);
        }

        public void CopyTo(IStorage otherStorage)
        {
            Exclusive(() =>
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

            });
        }

        public void Purge()
        {
            Exclusive(() =>
            {
                foreach (var directoryInfo in _dir.GetDirectories())
                    directoryInfo.Delete(true);

            });
        }

        public void Purge(Type type)
        {
            Exclusive(() =>
            {
                foreach (var directoryInfo in _dir.EnumerateDirectories(type.FullName))
                    directoryInfo.Delete(true);

            });
        }
    }
}