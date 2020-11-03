using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityAnimatorScriptOccurence : UnityAssetOccurrence
    {
        [NotNull] private IAnimatorScriptUsage Usage { get; }

        public UnityAnimatorScriptOccurence([NotNull] IPsiSourceFile sourceFile,
                                            [NotNull] IDeclaredElementPointer<IDeclaredElement> declaredElement,
                                            [NotNull] IAnimatorScriptUsage usage)
            : base(sourceFile, declaredElement, usage.Location, false)
        {
            Usage = usage;
        }

        public override RichText GetDisplayText()
        {
            var animatorContainer = GetSolution()
                .NotNull("occurrence.GetSolution() != null")
                .GetComponent<AnimatorScriptUsagesElementContainer>();
            var stateMachinePath = animatorContainer.GetStateMachinePathFor(Usage.Location);
            return stateMachinePath.IsEmpty() 
                ? new RichText($"{Usage.Name}") 
                : new RichText($"{stateMachinePath}/{Usage.Name}");
        }

        public override string GetRelatedFilePresentation()
        {
            return SourceFile.NotNull().Name.SubstringBeforeLast(".")?.SubstringAfterLast("/");
        }
        
        public override bool Navigate([NotNull] ISolution solution,
                                     [NotNull] PopupWindowContextSource windowContext,
                                     bool transferFocus,
                                     TabOptions tabOptions = TabOptions.Default)
        {
            if (DeclaredElementPointer is null) return true;
            var navigator = solution.GetComponent<UnityAssetOccurrenceNavigator>();
            return navigator.Navigate(solution, DeclaredElementPointer, OwningElementLocation);
        }

        public override string ToString()
        {
            return GetDisplayText()?.Text ??
                   (Usage is AnimatorStateScriptUsage ? "Animator state" : "Animator state machine");
        }
    }
}