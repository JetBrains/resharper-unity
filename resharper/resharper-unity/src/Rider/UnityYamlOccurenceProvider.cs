using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OccurrenceProvider(Priority = 10)]
    public class UnityYamlOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {
            if (findResult is FindResultReference findResultReference)
            {
                IUnityYamlReference reference = null;
                if (findResultReference.Reference is UnityEventTargetReference unityEventTargetReference)
                    reference = unityEventTargetReference;
                
                if (findResultReference.Reference is MonoScriptReference monoScriptReference)
                    reference = monoScriptReference;
                
                if (reference != null)
                {
                    var node = reference.GetTreeNode();
                    if (node.GetSolution().GetComponent<ConnectionTracker>().LastCheckResult != UnityEditorState.Disconnected)
                        return new UnityEditorOccurrence(reference.GetTreeNode(), reference, OccurrenceType.TextualOccurrence);
                }
            }
            
            return null;
        }
    }
}