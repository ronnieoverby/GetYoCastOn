//using System.IO;
//using System.Xml.Serialization;

//namespace SomeDB
//{
//    public class MyXmlSerializer : ISerializer
//    {
//        public T Deserialize<T>(Stream stream)
//        {
//            var ser = new XmlSerializer(typeof (T));
//            return (T) ser.Deserialize(stream);
//        }

//        public string Serialize(object value)
//        {
//            var ser = new XmlSerializer(value.GetType());
//            using (var writer = new StringWriter())
//            {
//                ser.Serialize(writer, value);
//                return writer.ToString();
//            }
//        }

//        public T Deserialize<T>(string s)
//        {
//            var ser = new XmlSerializer(typeof (T));
//            using (var reader = new StringReader(s))
//                return (T) ser.Deserialize(reader);
//        }
//    }
//}