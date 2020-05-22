using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    public class InspectorValuesBuildResult
    {
        public InspectorValuesBuildResult(LocalList<InspectorVariableUsage> inspectorValues, ImportedInspectorValues importedInspectorValues)
        {
            InspectorValues = inspectorValues;
            ImportedInspectorValues = importedInspectorValues;
        }

        public LocalList<InspectorVariableUsage> InspectorValues { get; }
        public ImportedInspectorValues ImportedInspectorValues { get; }
    }

    public class ImportedValueReference
    {
        public LocalReference LocalReference { get; }
        public string Name { get; }

        public ImportedValueReference(LocalReference localReference, string name)
        {
            LocalReference = localReference;
            Name = name;
        }

        public void WriteTo(UnsafeWriter writer)
        {
            LocalReference.WriteTo(writer);
            writer.Write(Name);
        }

        public static ImportedValueReference ReadFrom(UnsafeReader reader)
        {
            return new ImportedValueReference(HierarchyReferenceUtil.ReadLocalReferenceFrom(reader), reader.ReadString());
        }
    }
}