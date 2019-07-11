using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Features.Inspections.Resources;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Swa
{
    [PolymorphicMarshaller]
    public class GameObjectHierarchyElement : IUnityHierarchyElement
    {
        public FileID Id { get; }
        public FileID CorrespondingSourceObject { get; }
        public FileID PrefabInstance { get; }
        public bool IsStripped { get; }
        public string Name { get; }
        public IList<FileID> Components { get; }
        
        public GameObjectHierarchyElement(FileID id, FileID correspondingSourceObject, FileID prefabParentObject, bool isStripped, string name, IList<FileID> components)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject ?? FileID.Null;
            PrefabInstance = prefabParentObject ?? FileID.Null;
            IsStripped = isStripped;
            Name = name;
            Components = components;
        }
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as GameObjectHierarchyElement);

        private static GameObjectHierarchyElement Read(UnsafeReader reader)
        {
           return new GameObjectHierarchyElement(
               FileID.ReadFrom(reader),
               FileID.ReadFrom(reader),
               FileID.ReadFrom(reader),
               reader.ReadBool(),
               reader.ReadString(),
               ReadChildren(reader));
        }

        private static IList<FileID> ReadChildren(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new List<FileID>(count);
            for (var i = 0; i < count; i++) 
                result.Add(FileID.ReadFrom(reader));

            return result;
        }

        private static void Write(UnsafeWriter writer, GameObjectHierarchyElement value)
        {
            value.Id.WriteTo(writer);
            value.CorrespondingSourceObject.WriteTo(writer);
            value.PrefabInstance.WriteTo(writer);
            writer.Write(value.IsStripped);
            writer.Write(value.Name);
            
            
            writer.Write(value.Components.Count);
            foreach (var component in value.Components)
            {
                component.WriteTo(writer);
            }
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