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
        public FileID Id { get; }
        public FileID CorrespondingSourceObject { get; }
        public FileID PrefabInstance { get; }
        public bool IsStripped { get; }

        public FileID TransformParentId { get; }

        private readonly Dictionary<FileID, string> myNames;
        private readonly Dictionary<FileID, int?> myRootIndexes;

        public ModificationHierarchyElement(FileID id, FileID correspondingSourceObject, FileID prefabInstance, bool isStripped, FileID transformParentId, Dictionary<FileID, int?> rootIndexes, Dictionary<FileID, string> names)
        {
            Id = id;
            CorrespondingSourceObject = correspondingSourceObject ?? FileID.Null;
            PrefabInstance = prefabInstance  ?? FileID.Null;
            IsStripped = isStripped;
            TransformParentId = transformParentId ?? FileID.Null;
            myRootIndexes = rootIndexes;
            myNames = names;
        }

        public string GetName(FileID fileID)
        {
            return myNames.GetValueSafe(fileID);
        }
        
        public int? GetRootIndex(FileID fileID)
        {
            return myRootIndexes.GetValueSafe(fileID, null);
        }
        
        
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ModificationHierarchyElement);

        private static ModificationHierarchyElement Read(UnsafeReader reader)
        {
            return new ModificationHierarchyElement(
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                FileID.ReadFrom(reader),
                reader.ReadBool(),
                FileID.ReadFrom(reader),
                ReadDictionary(reader, unsafeReader =>
                {
                    if (unsafeReader.ReadNullness())
                        return (int?) unsafeReader.ReadInt32();
                    return null;
                }),
                ReadDictionary(reader, unsafeReader => unsafeReader.ReadString())
            );
        }

        private static void WriteDictionary<T>(UnsafeWriter writer, Dictionary<FileID, T> value, Action<UnsafeWriter, T> writeValue)
        {
            writer.Write(value.Count);
            foreach (var (id, v) in value)
            {
                id.WriteTo(writer);
                writeValue(writer, v);
            }
        }

        private static Dictionary<FileID, T> ReadDictionary<T>(UnsafeReader reader, Func<UnsafeReader, T> readValue)
        {
            var count = reader.ReadInt32();
            var result = new Dictionary<FileID, T>(count);
            for (int i = 0; i < count; i++)
            {
                result[FileID.ReadFrom(reader)] = readValue(reader);
            }

            return result;
        }
        
        private static IList<FileID> ReadChildren(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var result = new List<FileID>(count);
            for (var i = 0; i < count; i++) 
                result.Add(FileID.ReadFrom(reader));

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