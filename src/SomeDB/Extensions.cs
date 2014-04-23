using System;
using System.IO;

namespace SomeDB
{
    static class Extensions
    {
        public static FileInfo GetFile(this DirectoryInfo dir, string fileName)
        {
            if (dir == null) throw new ArgumentNullException("dir");
            return new FileInfo(Path.Combine(dir.FullName, fileName));
        }
    }
}
