using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class MonoBehaviourProperty
    {
        public readonly string Guid;
        public readonly string FieldName;

        public MonoBehaviourProperty(string guid, string fieldName)
        {
            Guid = guid;
            FieldName = fieldName;
        }


        public override int GetHashCode()
        {
            var hash = Hash.Create(FieldName);
            hash.PutString(Guid);
            return hash.Value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MonoBehaviourProperty mbProperty))
                return false;
            return Guid.Equals(mbProperty.Guid) && FieldName.Equals(mbProperty.FieldName);
        }
        
        public static void WriteTo(UnsafeWriter writer, MonoBehaviourProperty value)
        {
            writer.Write(value.Guid);
            writer.Write(value.FieldName);
        }

        public static MonoBehaviourProperty ReadFrom(UnsafeReader reader)
        {
            return new MonoBehaviourProperty(reader.ReadString(), reader.ReadString());
        }
    }
}