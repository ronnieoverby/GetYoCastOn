using System;

namespace SomeDB
{
    public class DocId : IEquatable<DocId>
    {
        public string Id { get; private set; }
        public Type Type { get; private set; }

        public DocId(IDocument doc)
            : this(doc.GetType(), doc.Id)
        {
        }

        public DocId(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            Type = type;
            Id = id;
        }

        public bool Equals(DocId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DocId left, DocId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DocId left, DocId right)
        {
            return !Equals(left, right);
        }
    }
}
