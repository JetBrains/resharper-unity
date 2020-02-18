using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods
{
    public class AssetMethodData
    {
        public LocalReference Location { get; }
        public string MethodName { get; }
        public EventHandlerArgumentMode Mode { get; }
        public string Type { get; }
        public IHierarchyReference TargetScriptReference { get; }
        public TextRange TextRange { get; }

        public AssetMethodData(LocalReference location, string methodName, TextRange textRange, EventHandlerArgumentMode mode, string type, IHierarchyReference targetReference)
        {
            Location = location;
            MethodName = methodName;
            TextRange = textRange;
            Mode = mode;
            Type = type;
            TargetScriptReference = targetReference;
        }

        public void WriteTo(UnsafeWriter writer)
        {
            writer.WritePolymorphic(Location);
            writer.Write(MethodName);
            writer.Write(TextRange.StartOffset);
            writer.Write(TextRange.EndOffset);
            writer.Write((int)Mode);
            writer.Write(Type);
            writer.WritePolymorphic(TargetScriptReference);
        }

        public static AssetMethodData ReadFrom(UnsafeReader reader)
        {
            return new AssetMethodData(reader.ReadPolymorphic<LocalReference>(), reader.ReadString(), new TextRange(reader.ReadInt32(), reader.ReadInt32()),
                (EventHandlerArgumentMode)reader.ReadInt32(), reader.ReadString(), reader.ReadPolymorphic<IHierarchyReference>());
        }

        protected bool Equals(AssetMethodData other)
        {
            return Equals(Location, other.Location) && MethodName == other.MethodName
                                                  && Mode == other.Mode
                                                  && Type == other.Type
                                                  && Equals(TargetScriptReference, other.TargetScriptReference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(AssetMethodData)) return false;
            return Equals((AssetMethodData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Location.GetHashCode();
                hashCode = (hashCode * 397) ^ MethodName.GetHashCode() ;
                hashCode = (hashCode * 397) ^ (int) Mode;
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TargetScriptReference.GetHashCode();
                return hashCode;
            }
        }
    }
}