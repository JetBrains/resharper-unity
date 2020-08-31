using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class AssetMethodUsages
    {
        public string OwnerName { get; }
        public string MethodName { get; }
        public EventHandlerArgumentMode Mode { get; }
        public string Type { get; }
        public IHierarchyReference TargetScriptReference { get; }
        public TextRange TextRangeOwnerPsiPersistentIndex { get; }
        
        public OWORD TextRangeOwner { get; }
        
        public AssetMethodUsages(string ownerName, string methodName, TextRange textRangeOwnerPsiPersistentIndex, OWORD textRangeOwner, EventHandlerArgumentMode mode, string type, IHierarchyReference targetReference)
        {
            Assertion.Assert(targetReference != null, "targetReference != null");
            Assertion.Assert(methodName != null, "methodName != null");
            OwnerName = ownerName;
            MethodName = methodName;
            TextRangeOwnerPsiPersistentIndex = textRangeOwnerPsiPersistentIndex;
            TextRangeOwner = textRangeOwner;
            Mode = mode;
            Type = type;
            TargetScriptReference = targetReference;
        }
        
        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(OwnerName);
            writer.Write(MethodName);
            writer.Write(TextRangeOwnerPsiPersistentIndex.StartOffset);
            writer.Write(TextRangeOwnerPsiPersistentIndex.EndOffset);
            AssetUtils.WriteOWORD(TextRangeOwner, writer);
            writer.Write((int)Mode);
            writer.Write(Type);
            TargetScriptReference.WriteTo(writer);
        }
        
        public static AssetMethodUsages ReadFrom(UnsafeReader reader)
        {
            return new AssetMethodUsages(reader.ReadString(),reader.ReadString(),
                new TextRange(reader.ReadInt32(), reader.ReadInt32()), AssetUtils.ReadOWORD(reader),
                (EventHandlerArgumentMode)reader.ReadInt32(), reader.ReadString(), HierarchyReferenceUtil.ReadReferenceFrom(reader));
        }

        protected bool Equals(AssetMethodUsages other)
        {
            return OwnerName == other.OwnerName &&
                   MethodName == other.MethodName && Mode == other.Mode && Type == other.Type &&
                   Equals(TargetScriptReference, other.TargetScriptReference) && TextRangeOwnerPsiPersistentIndex.Equals(other.TextRangeOwnerPsiPersistentIndex) &&
                   TextRangeOwner == other.TextRangeOwner;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetMethodUsages) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OwnerName.GetHashCode();
                hashCode = (hashCode * 397) ^ MethodName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Mode;
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TargetScriptReference.GetHashCode();
                hashCode = (hashCode * 397) ^ TextRangeOwnerPsiPersistentIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ TextRangeOwner.GetHashCode();
                return hashCode;
            }
        }
        
        [CanBeNull]
        public static AssetMethodUsages TryCreateAssetMethodFromModifications(LocalReference location, string unityEventName, Dictionary<string, IAssetValue> modifications, AssetMethodUsages source = null)
        {
            string name;
            TextRange textRange;
            OWORD textRangeOwner;
            if (!modifications.TryGetValue("m_MethodName", out var nameValue))
            {
                if (source == null)
                    return null;
                name = source.MethodName;
                textRange = source.TextRangeOwnerPsiPersistentIndex;
                textRangeOwner = source.TextRangeOwner;
            }
            else
            {
                name = (nameValue as AssetSimpleValue).NotNull("name != null").SimpleValue;

                var range = (modifications["m_MethodNameRange"] as Int2Value).NotNull("range != null");
                textRange = new TextRange(range.X, range.Y);
                textRangeOwner = location.OwningPsiPersistentIndex;
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

            return new AssetMethodUsages(unityEventName, name, textRange, textRangeOwner, mode, null, target);
        }

        public Dictionary<string, IAssetValue> ToDictionary()
        {
            var dictionary = new Dictionary<string, IAssetValue>();
            
            dictionary["m_Mode"] = new AssetSimpleValue(((int)Mode).ToString());
            dictionary["m_MethodName"] = new AssetSimpleValue(MethodName);
            dictionary["m_MethodNameRange"] = new Int2Value(TextRangeOwnerPsiPersistentIndex.StartOffset, TextRangeOwnerPsiPersistentIndex.EndOffset);
            dictionary["m_Target"] = new AssetReferenceValue(TargetScriptReference);
            return dictionary;
        }
    }
}