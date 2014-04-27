using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SomeDB.Storage
{
    public class FileSystemStorage : IPersistentStorage
    {
        private readonly ISerializer _serializer;
        private readonly DirectoryInfo _dir;

        private readonly ReaderWriterLockSlim _masterLock = new ReaderWriterLockSlim();

        private const int FileLockCap = 100000;
        private readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _fileLocks =
            new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);

        public FileSystemStorage(string rootPath = null, ISerializer serializer = null)
        {
            var di = new DirectoryInfo(rootPath ?? this.GetDefaultDirectory());
            di.Create();
            _dir = di;
            _serializer = serializer ?? new JsonSerializer();
        }

        public IEnumerable<IDocument> Store(IEnumerable<IDocument> documents)
        {
            foreach (var document in documents)
            {
                var type = document.GetType();

                var file = _dir.CreateSubdirectory(type.FullName)
                    .GetFile(document.Id);

                var serialized = _serializer.Serialize(document);

                Write(file.FullName, serialized);
                yield return document;
            }
        }

        public IDocument Retrieve(Type type, string id)
        {
            var file = _dir.CreateSubdirectory(type.FullName)
                .GetFile(id);

            var serialized = Read(file.FullName);
            var document = (IDocument)_serializer.Deserialize(serialized, type);
            return document;
        }

        public IEnumerable<IDocument> RetrieveAll(Type type)
        {
            var dir = _dir.CreateSubdirectory(type.FullName);
            return
                dir.EnumerateFiles().Select(file =>
                {
                    var serialized = Read(file.FullName);
                    var doc = _serializer.Deserialize(serialized, type);
                    return doc;
                }).Cast<IDocument>();
        }

        public IEnumerable<IDocument> RetrieveAll()
        {
            var allTypes = AppDomain.CurrentDomain.GetTypes()
                       .ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase);

            return from dir in _dir.GetDirectories()
                let type = allTypes[dir.Name].Single()
                where type != null
                from file in dir.EnumerateFiles()
                let value = Read(file.FullName)
                let doc =(IDocument) _serializer.Deserialize(value, type)
                select doc;
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

    }
}