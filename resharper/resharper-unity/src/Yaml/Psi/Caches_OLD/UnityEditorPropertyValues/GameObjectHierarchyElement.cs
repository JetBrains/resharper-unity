using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Features.Inspections.Resources;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PolymorphicMarshaller]
    public class GameObjectHierarchyElement : IUnityHierarchyElement
    {
        public AssetDocumentReference Id { get; }
        public AssetDocumentReference CorrespondingSourceObject { get; }
        public AssetDocumentReference PrefabInstance { get; }
        public bool IsStripped { get; }
        public string Name { get; }
        public AssetDocumentReference TransformId { get; set; }

        public GameObjectHierarchyElement(AssetDocumentReference id, AssetDocumentReference correspondingSourceObject, AssetDocumentReference prefabParentObject, bool isStripped, AssetDocumentReference transformId, string name)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject ?? AssetDocumentReference.Null;
            PrefabInstance = prefabParentObject ?? AssetDocumentReference.Null;
            IsStripped = isStripped;
            TransformId = transformId ?? AssetDocumentReference.Null;
            Name = name;
        }
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as GameObjectHierarchyElement);

        private static GameObjectHierarchyElement Read(UnsafeReader reader)
        {
           return new GameObjectHierarchyElement(
               AssetDocumentReference.ReadFrom(reader),
               AssetDocumentReference.ReadFrom(reader),
               AssetDocumentReference.ReadFrom(reader),
               reader.ReadBool(),
               AssetDocumentReference.ReadFrom(reader), 
               reader.ReadString());
        }

        private static void Write(UnsafeWriter writer, GameObjectHierarchyElement value)
        {
            value.Id.WriteTo(writer);
            value.CorrespondingSourceObject.WriteTo(writer);
            value.PrefabInstance.WriteTo(writer);
            writer.Write(value.IsStripped);
            value.TransformId.WriteTo(writer);
            writer.Write(value.Name);
        }

        protected bool Equals(GameObjectHierarchyElement other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameObjectHierarchyElement) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}