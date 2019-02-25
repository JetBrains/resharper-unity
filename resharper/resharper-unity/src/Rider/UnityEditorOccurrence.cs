using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityEditorOccurrence : ReferenceOccurrence
    {
        private readonly IUnityYamlReference myUnityEventTargetReference;
        public UnityEditorOccurrence([NotNull] IUnityYamlReference unityEventTargetReference, IDeclaredElement element,
            OccurrenceType occurrenceType)
            : base(unityEventTargetReference, element, occurrenceType)
        {
            myUnityEventTargetReference = unityEventTargetReference;
        }

        public override bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            if (!solution.GetComponent<ConnectionTracker>().IsConnectionEstablished())
                return base.Navigate(solution, windowContext, transferFocus, tabOptions);
            
            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            return findRequestCreator.CreateRequestToUnity(myUnityEventTargetReference, true);
        }
    }
}