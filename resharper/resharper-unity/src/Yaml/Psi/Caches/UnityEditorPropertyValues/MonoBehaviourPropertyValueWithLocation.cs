using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class MonoBehaviourPropertyValueWithLocation
    {
        [CanBeNull] public string PrimitiveValue { get; }
        [CanBeNull] public FileID Reference { get; }

        [NotNull] public FileID GameObject { get; }
        
        public MonoBehaviourPropertyValueWithLocation([NotNull] string primitiveValue, [NotNull] FileID gameObject)
        {
            PrimitiveValue = primitiveValue;
            GameObject = gameObject;
        }

        public MonoBehaviourPropertyValueWithLocation([NotNull] FileID reference, [NotNull] FileID gameObject)
        {
            Reference = reference;
            GameObject = gameObject;
        }
        
        public void WriteTo(UnsafeWriter writer)
        {
            GameObject.WriteTo(writer);
            if (Reference == null)
            {
                writer.Write(true);
                writer.Write(PrimitiveValue);
            }
            else
            {
                writer.Write(false);
                Reference.WriteTo(writer);
            }
        }

        public static MonoBehaviourPropertyValueWithLocation ReadFrom(UnsafeReader reader)
        {
            var go = FileID.ReadFrom(reader);
            var type = reader.ReadBool();
            return type
                ? new MonoBehaviourPropertyValueWithLocation(reader.ReadString().NotNull("primitiveValue != null"), go)
                : new MonoBehaviourPropertyValueWithLocation(FileID.ReadFrom(reader), go);
        }

    }
}