using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityAnimatorScriptOccurence : UnityAssetOccurrence
    {
        [NotNull] private IAnimatorScriptUsage Usage { get; }
        [CanBeNull] private IconId IconId { get; }
        [NotNull] private AnimatorScriptUsagesElementContainer Container { get; }

        public UnityAnimatorScriptOccurence([NotNull] IPsiSourceFile sourceFile,
                                            [NotNull] IDeclaredElementPointer<IDeclaredElement> declaredElement,
                                            [NotNull] IAnimatorScriptUsage usage)
            : base(sourceFile, declaredElement, usage.Location, false)
        {
            Usage = usage;
            var container = GetSolution()
                .NotNull("occurrence.GetSolution() != null")
                .GetComponent<AnimatorScriptUsagesElementContainer>();
            Container = container;
            var element = declaredElement.FindDeclaredElement();
            if (element is null) return;
            var location = Usage.Location;
            container.GetElementsNames(location, element, out _, out var isStateMachine);
            IconId = isStateMachine
                ? UnityObjectTypeThemedIcons.UsageAnimatorStateMachine.Id
                : UnityObjectTypeThemedIcons.UsageAnimatorState.Id;
        }

        public override RichText GetDisplayText()
        {
            var stateMachinePath = Container.GetStateMachinePathFor(Usage.Location);
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

        public override IconId GetIcon()
        {
            return IconId ?? base.GetIcon();
        }
    }
}