using System;
using JetBrains.Annotations;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public static class HierarchyReferenceUtil
    {
        public static void WriteTo([NotNull] this IHierarchyReference reference, UnsafeWriter writer)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            
            if (reference is LocalReference localReference)
                localReference.WriteTo(writer);
            else if (reference is ExternalReference externalReference)
                externalReference.WriteTo(writer);
            else
                throw new InvalidOperationException("Unexpected reference type");
        }

        public static void WriteTo(this LocalReference localReference, UnsafeWriter writer)
        {
            writer.Write(0);
            AssetUtils.WriteOWORD(localReference.OwningPsiPersistentIndex, writer);    
            writer.Write(localReference.LocalDocumentAnchor);
        }
        
        public static void WriteTo(this ExternalReference externalReference, UnsafeWriter writer)
        {
            writer.Write(1);
            writer.Write(externalReference.ExternalAssetGuid);
            writer.Write(externalReference.LocalDocumentAnchor);

        }

        public static ExternalReference ReadExternalReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id != 1)
                throw new InvalidOperationException($"Expected external reference, found {id}");
            return new ExternalReference(reader.ReadGuid(), reader.ReadLong());
        }

        public static LocalReference ReadLocalReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id != 0)
                throw new InvalidOperationException($"Expected local reference, found {id}");
            return new LocalReference(AssetUtils.ReadOWORD(reader), reader.ReadLong());
        }

        public static IHierarchyReference ReadReferenceFrom(UnsafeReader reader)
        {
            var id = reader.ReadInt();
            if (id == 0)
                return new LocalReference(AssetUtils.ReadOWORD(reader), reader.ReadLong());
            if (id == 1)
                return new ExternalReference(reader.ReadGuid(), reader.ReadLong());
            
            throw new InvalidOperationException($"Unknown reference type, {id}");
        }
    }
}