using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SomeDB
{
    static class Extensions
    {
        public static FileInfo GetFile(this DirectoryInfo dir, string fileName)
        {
            if (dir == null) throw new ArgumentNullException("dir");
            return new FileInfo(Path.Combine(dir.FullName, fileName));
        }

        public static Type[] GetAllSubTypes(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes().Where(x => x != type).Where(type.IsAssignableFrom);
                    }
                    catch (Exception)
                    {
                        return new Type[0];
                    }
                }).ToArray();
        }
    }
}
