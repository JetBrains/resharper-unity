using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class MonoScriptReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<MonoScriptReference>(oldReferences, element))
                return oldReferences;

            if (!(element is IPlainScalarNode guidValue))
                return ReferenceCollection.Empty;

            // m_Script: {fileID: 11500000, guid: xxx, type: x}
            var guidEntry = FlowMapEntryNavigator.GetByValue(guidValue);
            var flowIDMap = FlowMappingNodeNavigator.GetByEntrie(guidEntry);
            var blockMappingEntry = BlockMappingEntryNavigator.GetByValue(flowIDMap);

            if (guidEntry?.Key.MatchesPlainScalarText("guid") == true
                && blockMappingEntry?.Key.MatchesPlainScalarText("m_Script") == true)
            {
                var fileID = flowIDMap.AsFileID();
                if (fileID != null && !fileID.IsNullReference && fileID.IsMonoScript)
                {
                    var metaGuidCache = element.GetSolution().GetComponent<MetaFileGuidCache>();
                    var reference = new MonoScriptReference(guidValue, fileID, metaGuidCache);
                    return new ReferenceCollection(reference);
                }
            }

            return ReferenceCollection.Empty;
        }

        // Names is likely to contain the name of the class. All we have in the file is the guid
        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var guidValue = element as IPlainScalarNode;
            var guidEntry = FlowMapEntryNavigator.GetByValue(guidValue);
            var flowIDMap = FlowMappingNodeNavigator.GetByEntrie(guidEntry);
            var blockMappingEntry = BlockMappingEntryNavigator.GetByValue(flowIDMap);
            return guidEntry?.Key.MatchesPlainScalarText("guid") == true
                   && blockMappingEntry?.Key.MatchesPlainScalarText("m_Script") == true;
        }
    }
}