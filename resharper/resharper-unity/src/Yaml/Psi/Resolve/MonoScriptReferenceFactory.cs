using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class MonoScriptReferenceFactory : IReferenceFactory
    {
        // If the document contains "m_Script: {fileID: 11500000, guid:", it's a reference to a class. This is fragile
        // if Unity starts to format its files differently, but I think this is ok
        private static readonly StringSearcher ourScriptReferenceStringSearcher =
            new StringSearcher("m_Script:", true);

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<MonoScriptReference>(oldReferences, element))
                return oldReferences;

            if (!(element is IPlainScalarNode guidValue))
                return ReferenceCollection.Empty;

            // m_Script: {fileID: 11500000, guid: xxx, type: x}
            var guidEntry = FlowMapEntryNavigator.GetByValue(guidValue);
            var flowIDMap = FlowMappingNodeNavigator.GetByEntrie(guidEntry);
            var blockMappingEntry = BlockMappingEntryNavigator.GetByContent(ContentNodeNavigator.GetByValue(flowIDMap));

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
            var blockMappingEntry = BlockMappingEntryNavigator.GetByContent(ContentNodeNavigator.GetByValue(flowIDMap));
            return guidEntry?.Key.MatchesPlainScalarText("guid") == true
                   && blockMappingEntry?.Key.MatchesPlainScalarText("m_Script") == true;
        }
        
        public static bool CanContainReference([NotNull] IYamlDocument document)
        {
            var buffer = document.GetTextAsBuffer();
            return CanContainReference(buffer);
        }

        
        public static bool CanContainReference(IBuffer bodyBuffer)
        {
            return ourScriptReferenceStringSearcher.Find(bodyBuffer) >= 0;
        }
    }
}