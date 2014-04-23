using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SomeDB
{
    static class IdDeriver
    {
        /// <summary>
        /// Get's an id for the object
        /// Sets the Id property if its not already set.
        /// </summary>
        public static object DeriveId<T>(this T obj)
        {
            var type = obj.GetType();

            var idProp =
                type.GetProperties().FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            if (idProp == null)
            {
                // there is no id property
                // create an id based on the object's state
                return DeriveIdFromState(obj);
            }

            // there is an id property
            // set the id if it is null/empty
            // otherwise just return it

            // for strings -> string.isNullorWhitespace(id) -> Guid.newGuid()
            // for guids -> id == guid.empty
            // dont attempt to set for other types

            object id = null;
            if (idProp.CanRead)
                id = idProp.GetValue(obj, null);

            if (idProp.PropertyType == typeof (string))
            {
                if (!string.IsNullOrWhiteSpace((string) id)) 
                    return id;

                id = Guid.NewGuid().ToString();

                if (idProp.CanWrite)
                    idProp.SetValue(obj, id, null);
            }

            else if (idProp.PropertyType == typeof (Guid) || idProp.PropertyType == typeof(Guid?))
            {
                if ((id != null && Guid.Empty != (Guid) id)) 
                    return id;

                id = Guid.NewGuid();

                if (idProp.CanWrite)
                    idProp.SetValue(obj, id, null);
            }

            return id ?? Guid.NewGuid();
        }

        internal static object DeriveIdFromState<T>(T item)
        {
            var type = typeof(T);
            var ser = new XmlSerializer(type);
            using (var writer = new StringWriter())
            {
                ser.Serialize(writer, item);
                return writer.ToString().CreateDeterministicGuid();
            }
        }
    }
}
