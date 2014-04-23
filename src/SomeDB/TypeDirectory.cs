using System;
using System.IO;

namespace SomeDB
{
    internal class TypeDirectory
    {
        public TypeDirectory(DirectoryInfo baseDir, Type type)
        {
            Directory = baseDir.CreateSubdirectory(type.FullName);
            Type = type;
        }

        public DirectoryInfo Directory { get; private set; }
        public Type Type { get; private set; }
    }
}