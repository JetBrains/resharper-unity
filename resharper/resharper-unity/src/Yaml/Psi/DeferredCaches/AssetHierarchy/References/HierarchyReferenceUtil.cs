using System;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public static class HierarchyReferenceUtil
    {
        public static void WriteTo(this IHierarchyReference reference, UnsafeWriter writer)
        {
            if (reference == null || reference is NullReference)
                writer.Write(0);
            else if (reference is LocalReference localReference)
                localReference.WriteTo(writer);
            else if (reference is ExternalReference externalReference)
                externalReference.WriteTo(writer);
            else
                throw new InvalidOperationException("Unexpected reference type");
        }

        public static void WriteTo(this LocalReference localReference, UnsafeWriter writer)
        {
            writer.Write(1);
            writer.Write(localReference.OwnerId);      
            writer.Write(localReference.LocalDocumentAnchor);
        }
        
        public static void WriteTo(this ExternalReference externalReference, UnsafeWriter writer)
        {
            writer.Write(2);
            writer.Write(externalReference.ExternalAssetGuid);
            writer.Write(externalReference.LocalDocumentAnchor);

        }

        public static ExternalReference ReadExternalReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id != 2)
                throw new InvalidOperationException($"Expected external reference, found {id}");
            return new ExternalReference(reader.ReadGuid(), reader.ReadULong());
        }

        public static LocalReference ReadLocalReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id != 1)
                throw new InvalidOperationException($"Expected local reference, found {id}");
            return new LocalReference(reader.ReadLong(), reader.ReadULong());
        }

        public static IHierarchyReference ReadReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id == 0)
                return new NullReference();
            if (id == 1)
                return new LocalReference(reader.ReadLong(), reader.ReadULong());
            if (id == 2)
                return new ExternalReference(reader.ReadGuid(), reader.ReadULong());
            
            throw new InvalidOperationException($"Unknown reference type, {id}");
        }
    }
}