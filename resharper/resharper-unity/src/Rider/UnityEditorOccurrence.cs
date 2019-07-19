using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityEditorOccurrence : ReferenceOccurrence
    {
        public readonly string Anchor;
        private readonly UnitySceneDataLocalCache myUnitySceneDataLocalCache;

        public UnityEditorOccurrence([NotNull] UnityPropertyValueCache unityPropertyValueCache, [NotNull] IUnityYamlReference unityEventTargetReference, IDeclaredElement element,
            OccurrenceType occurrenceType)
            : base(unityEventTargetReference, element, occurrenceType)
        {
            myUnitySceneDataLocalCache = unityPropertyValueCache.UnitySceneDataLocalCache;
            Anchor = UnityGameObjectNamesCache.GetAnchorFromBuffer(unityEventTargetReference.ComponentDocument.GetTextAsBuffer());
        }

        public override bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            if (!solution.GetComponent<ConnectionTracker>().IsConnectionEstablished())
                return base.Navigate(solution, windowContext, transferFocus, tabOptions);
            
            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            var reference = PrimaryReference as IUnityYamlReference;
            if (reference == null)
                return true;
            findRequestCreator.CreateRequestToUnity(reference, true);
            return true;
        }
    }
}