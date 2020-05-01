using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class AssetMethodData
    {
        public LocalReference OwnerLocation { get; }
        public string OwnerName { get; }
        public string MethodName { get; }
        public EventHandlerArgumentMode Mode { get; }
        public string Type { get; }
        public IHierarchyReference TargetScriptReference { get; }
        public TextRange TextRange { get; }
        
        public long TextRangeOwner { get; }
        
        public AssetMethodData(LocalReference ownerLocation, string ownerName, string methodName, TextRange textRange, long textRangeOwner, EventHandlerArgumentMode mode, string type, IHierarchyReference targetReference)
        {
            Assertion.Assert(targetReference != null, "targetReference != null");
            Assertion.Assert(methodName != null, "methodName != null");
            OwnerLocation = ownerLocation;
            OwnerName = ownerName;
            MethodName = methodName;
            TextRange = textRange;
            TextRangeOwner = textRangeOwner;
            Mode = mode;
            Type = type;
            TargetScriptReference = targetReference;
        }
        
        public void WriteTo(UnsafeWriter writer)
        {
            OwnerLocation.WriteTo(writer);
            writer.Write(OwnerName);
            writer.Write(MethodName);
            writer.Write(TextRange.StartOffset);
            writer.Write(TextRange.EndOffset);
            writer.Write(TextRangeOwner);
            writer.Write((int)Mode);
            writer.Write(Type);
            TargetScriptReference.WriteTo(writer);
        }
        
        public static AssetMethodData ReadFrom(UnsafeReader reader)
        {
            return new AssetMethodData(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), reader.ReadString(),reader.ReadString(),
                new TextRange(reader.ReadInt32(), reader.ReadInt32()), reader.ReadLong(),
                (EventHandlerArgumentMode)reader.ReadInt32(), reader.ReadString(), HierarchyReferenceUtil.ReadReferenceFrom(reader));
        }

        protected bool Equals(AssetMethodData other)
        {
            return OwnerLocation.Equals(other.OwnerLocation) && OwnerName == other.OwnerName &&
                   MethodName == other.MethodName && Mode == other.Mode && Type == other.Type &&
                   Equals(TargetScriptReference, other.TargetScriptReference) && TextRange.Equals(other.TextRange) &&
                   TextRangeOwner == other.TextRangeOwner;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetMethodData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OwnerLocation.GetHashCode();
                hashCode = (hashCode * 397) ^ OwnerName.GetHashCode();
                hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Mode;
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TargetScriptReference.GetHashCode();
                hashCode = (hashCode * 397) ^ TextRange.GetHashCode();
                hashCode = (hashCode * 397) ^ TextRangeOwner.GetHashCode();
                return hashCode;
            }
        }
        
        [CanBeNull]
        public static AssetMethodData TryCreateAssetMethodFromModifications(LocalReference location, string unityEventName, Dictionary<string, IAssetValue> modifications, AssetMethodData source = null)
        {
            string name;
            TextRange textRange;
            long textRangeOwner;
            if (!modifications.TryGetValue("m_MethodName", out var nameValue))
            {
                if (source == null)
                    return null;
                name = source.MethodName;
                textRange = source.TextRange;
                textRangeOwner = source.TextRangeOwner;
            }
            else
            {
                name = (nameValue as AssetSimpleValue).NotNull("name != null").SimpleValue;

                var range = (modifications["m_MethodNameRange"] as Int2Value).NotNull("range != null");
                textRange = new TextRange(range.X, range.Y);
                textRangeOwner = location.OwnerId;
            }

            EventHandlerArgumentMode mode;
            if (!modifications.TryGetValue("m_Mode", out var modeValue))
            {
                if (source == null)
                    return null;
                mode = source.Mode;
            }
            else
            {
                if (!int.TryParse((modeValue as AssetSimpleValue).NotNull("modeValue as AssetSimpleValue != null").SimpleValue, out var modeV))
                    return null;

                mode = (EventHandlerArgumentMode) modeV;
            }

            IHierarchyReference target;
            if (!modifications.TryGetValue("m_Target", out var referenceValue))
            {
                if (source == null)
                    return null;
                target = source.TargetScriptReference;
            }
            else
            {
                target = (referenceValue as AssetReferenceValue).NotNull("referenceValue as AssetReferenceValue != null").Reference;            
            }

            return new AssetMethodData(location, unityEventName, name, textRange, textRangeOwner, mode, null, target);
        }

        public Dictionary<string, IAssetValue> ToDictionary()
        {
            var dictionary = new Dictionary<string, IAssetValue>();
            
            dictionary["m_Mode"] = new AssetSimpleValue(((int)Mode).ToString());
            dictionary["m_MethodName"] = new AssetSimpleValue(MethodName);
            dictionary["m_MethodNameRange"] = new Int2Value(TextRange.StartOffset, TextRange.EndOffset);
            dictionary["m_Target"] = new AssetReferenceValue(TargetScriptReference);
            return dictionary;
        }
    }
}