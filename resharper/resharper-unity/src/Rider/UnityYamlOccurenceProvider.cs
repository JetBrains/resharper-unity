using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OccurrenceProvider(Priority = 10)]
    public class UnityYamlOccurenceProvider : IOccurrenceProvider
    {
        public IOccurrence MakeOccurrence(FindResult findResult)
        {
            if (findResult is IFindResultReference findResultReference)
            {
                IUnityYamlReference reference = null;
                if (findResultReference.Reference is UnityEventTargetReference unityEventTargetReference)
                    reference = unityEventTargetReference;

                if (findResultReference.Reference is MonoScriptReference monoScriptReference)
                    reference = monoScriptReference;

                if (reference != null)
                {
                    return new UnityEditorOccurrence(reference, findResultReference.DeclaredElement, OccurrenceType.TextualOccurrence);
                }
            }

            return null;
        }
    }
}