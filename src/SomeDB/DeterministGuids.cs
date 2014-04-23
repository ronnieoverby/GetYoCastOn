using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Logos.Utility;

namespace SomeDB
{
    internal static class DeterministGuids
    {
        public static readonly Guid NamespaceId =
            GetCustomAttribute();

        private static Guid GetCustomAttribute()
        {
            var assembly = Assembly.GetCallingAssembly();
            var attributes = assembly.GetCustomAttributes(typeof (GuidAttribute), false);
            var o = (GuidAttribute) attributes[0];
            return new Guid(o.Value);
        }

        public static Guid CreateDeterministicGuid(this string s)
        {
            return GuidUtility.Create(NamespaceId, s);
        }
    }
}