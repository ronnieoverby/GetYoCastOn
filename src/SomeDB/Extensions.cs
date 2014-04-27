using System;
using System.IO;
using System.Linq;
using SomeDB.Storage;

namespace SomeDB
{
    static class Extensions
    {
        public static FileInfo GetFile(this DirectoryInfo dir, string fileName)
        {
            if (dir == null) throw new ArgumentNullException("dir");
            return new FileInfo(Path.Combine(dir.FullName, fileName));
        }

        public static Type[] GetTypes(this AppDomain appDomain)
        {
            if (appDomain == null) throw new ArgumentNullException("appDomain");

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (Exception)
                    {
                        return new Type[0];
                    }
                }).ToArray();
        }

        public static Type[] GetAllSubTypes(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return AppDomain.CurrentDomain.GetTypes().Where(x => x != type).Where(type.IsAssignableFrom)
                .ToArray();
        }

        public static Type[] GetAllSuperTypes(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return AppDomain.CurrentDomain.GetTypes().Where(x => x != type).Where(t => t.IsAssignableFrom(type))
                .ToArray();
        }

        public static string GetDefaultDirectory(this IPersistentStorage stg)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appName = AppDomain.CurrentDomain.FriendlyName;
            return Path.Combine(appData, appName, "data");
        }

        public static void CopyTo(this IStorage source, IStorage destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            destination.Store(source.RetrieveAll());
        }


        internal static void Record(this Stats stats, string name, TimeSpan elapsed)
        {
            if (stats == null) return;
            stats.AddOrUpdate(name, n => new Stats.Stat(name, elapsed), (n, stat) => stat.Record(elapsed));
        }
    }
}
