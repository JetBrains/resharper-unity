using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PolymorphicMarshaller]
    public class ModificationHierarchyElement : IUnityHierarchyElement
    {
        public AssetDocumentReference Id { get; }
        public AssetDocumentReference CorrespondingSourceObject { get; }
        public AssetDocumentReference PrefabInstance { get; }
        public bool IsStripped { get; }

        public AssetDocumentReference TransformParentId { get; }

        private readonly Dictionary<AssetDocumentReference, string> myNames;
        private readonly Dictionary<AssetDocumentReference, int?> myRootIndexes;

        public ModificationHierarchyElement(AssetDocumentReference id, AssetDocumentReference correspondingSourceObject, AssetDocumentReference prefabInstance, bool isStripped, AssetDocumentReference transformParentId, Dictionary<AssetDocumentReference, int?> rootIndexes, Dictionary<AssetDocumentReference, string> names)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject ?? AssetDocumentReference.Null;
            PrefabInstance = prefabInstance  ?? AssetDocumentReference.Null;
            IsStripped = isStripped;
            TransformParentId = transformParentId ?? AssetDocumentReference.Null;
            myRootIndexes = rootIndexes;
            myNames = names;
        }

        public string GetName(AssetDocumentReference assetDocumentReference)
        {
            return myNames.GetValueSafe(assetDocumentReference);
        }
        
        public int? GetRootIndex(AssetDocumentReference assetDocumentReference)
        {
            return myRootIndexes.GetValueSafe(assetDocumentReference, null);
        }
        
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ModificationHierarchyElement);

        private static ModificationHierarchyElement Read(UnsafeReader reader)
        {
            return new ModificationHierarchyElement(
                AssetDocumentReference.ReadFrom(reader),
                AssetDocumentReference.ReadFrom(reader),
                AssetDocumentReference.ReadFrom(reader),
                reader.ReadBool(),
                AssetDocumentReference.ReadFrom(reader),
                ReadDictionary(reader, unsafeReader =>
                {
                    if (unsafeReader.ReadNullness())
                        return (int?) unsafeReader.ReadInt32();
                    return null;
                }),
                ReadDictionary(reader, unsafeReader => unsafeReader.ReadString())
            );
        }

        private static void WriteDictionary<T>(UnsafeWriter writer, Dictionary<AssetDocumentReference, T> value, Action<UnsafeWriter, T> writeValue)
        {
            writer.Write(value.Count);
            foreach (var (id, v) in value)
            {
                id.WriteTo(writer);
                writeValue(writer, v);
            }
        }

        private static Dictionary<AssetDocumentReference, T> ReadDictionary<T>(UnsafeReader reader, Func<UnsafeReader, T> readValue)
        {
            var count = reader.ReadInt32();
            var result = new Dictionary<AssetDocumentReference, T>(count);
            for (int i = 0; i < count; i++)
            {
                result[AssetDocumentReference.ReadFrom(reader)] = readValue(reader);
            }

            return result;
        }
        
        private static IList<AssetDocumentReference> ReadChildren(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new List<AssetDocumentReference>(count);
            for (var i = 0; i < count; i++) 
                result.Add(AssetDocumentReference.ReadFrom(reader));

            return result;
        }

        private static void Write(UnsafeWriter writer, ModificationHierarchyElement value)
        {
            value.Id.WriteTo(writer);
            value.CorrespondingSourceObject.WriteTo(writer);
            value.PrefabInstance.WriteTo(writer);
            writer.Write(value.IsStripped);
            value.TransformParentId.WriteTo(writer);
            WriteDictionary(writer, value.myRootIndexes, (w, v) =>
            {
                if (w.WriteNullness(v))
                    w.Write(v.Value);
            });
            
            WriteDictionary(writer, value.myNames, (w, v) => w.Write(v));
        }

        protected bool Equals(ModificationHierarchyElement other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModificationHierarchyElement) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}