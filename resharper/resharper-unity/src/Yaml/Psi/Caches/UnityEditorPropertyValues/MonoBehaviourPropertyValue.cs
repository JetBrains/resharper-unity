using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{

    public class MonoBehaviourPrimitiveValue : MonoBehaviourPropertyValue
    {
        [NotNull]
        public string PrimitiveValue { get; }

        public MonoBehaviourPrimitiveValue([NotNull] string primitiveValue, [NotNull] string monoBehaviour)
            : base(monoBehaviour)
        {
            PrimitiveValue = primitiveValue;
        }
        
        protected bool Equals(MonoBehaviourPrimitiveValue other)
        {
            return base.Equals(other) && string.Equals(PrimitiveValue, other.PrimitiveValue);
        }
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MonoBehaviourPrimitiveValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ PrimitiveValue.GetHashCode();
            }
        }

        internal override void WriteTo(UnsafeWriter writer)
        {
            writer.Write(true);
            writer.Write(PrimitiveValue);
            base.WriteTo(writer);
        }

        public static MonoBehaviourPropertyValue ReadFrom(UnsafeReader reader)
        {
            return new MonoBehaviourPrimitiveValue(reader.ReadString().NotNull("primitiveValue != null"), reader.ReadString().NotNull("monoBehaviour != null"));
        }
    }

    public class MonoBehaviourReferenceValue : MonoBehaviourPropertyValue {
    
        [NotNull]
        public FileID Reference { get; }

        public MonoBehaviourReferenceValue([NotNull] FileID referenceValue, [NotNull] string monoBehaviour)
            : base(monoBehaviour)
        {
            Reference = referenceValue;
        }

        protected bool Equals(MonoBehaviourReferenceValue other)
        {
            return base.Equals(other) && Reference.Equals(other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MonoBehaviourReferenceValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Reference.GetHashCode();
            }
        }

        public static MonoBehaviourPropertyValue ReadFrom(UnsafeReader reader)
        {
            return new MonoBehaviourReferenceValue(FileID.ReadFrom(reader), reader.ReadString().NotNull("monoBehaviour != null"));
        }

        internal override void WriteTo(UnsafeWriter writer)
        {
            writer.Write(false);
            Reference.WriteTo(writer);
            base.WriteTo(writer);
        }
    }
    
    public abstract class MonoBehaviourPropertyValue
    {
        [NotNull]
        public string MonoBehaviour { get; }

        public MonoBehaviourPropertyValue([NotNull] string monoBehaviour)
        {
            MonoBehaviour = monoBehaviour;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is MonoBehaviourPropertyValue value))
                return false;

            return MonoBehaviour.Equals(value.MonoBehaviour);
        }

        protected bool Equals(MonoBehaviourPropertyValue other)
        {
            return MonoBehaviour.Equals(other.MonoBehaviour);
        }

        public override int GetHashCode()
        {
            return MonoBehaviour.GetHashCode();
        }

        internal virtual void WriteTo(UnsafeWriter writer)
        {
            writer.Write(MonoBehaviour);
        }
    }

    public static class MonoBehaviourPropertyValueMarshaller
    {
        public static MonoBehaviourPropertyValue Read(UnsafeReader reader)
        {
            var isPrimitive = reader.ReadBool();
            return isPrimitive
                ? MonoBehaviourPrimitiveValue.ReadFrom(reader)
                : MonoBehaviourReferenceValue.ReadFrom(reader);
        }

        public static void Write(UnsafeWriter writer, MonoBehaviourPropertyValue value)
        {
            value.WriteTo(writer);
        }
    }
}