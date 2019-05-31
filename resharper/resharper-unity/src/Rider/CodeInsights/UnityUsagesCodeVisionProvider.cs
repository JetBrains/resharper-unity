using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Host.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.VB.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [ShellComponent]
    public class UnityUsagesCodeVisionProvider : ContextNavigationCodeInsightsProviderBase<GoToUnityUsagesAction, GoToUnityUsagesProvider>
    {
        private readonly UnityEditorUsageCounter myUnityEditorUsageCounter;

        public UnityUsagesCodeVisionProvider(Shell shell) : base(shell)
        {
            myUnityEditorUsageCounter = shell.GetComponent<UnityEditorUsageCounter>();
        }
        
        protected override string Noun(IDeclaredElement element, int count) => "asset usage" + Arity(count);

        protected override int GetBaseCount(SolutionAnalysisService swa, IGlobalUsageChecker usageChecker, IDeclaredElement element,
            ElementId? elementId)
        {
            Assertion.Assert(elementId.HasValue, "elementId.HasValue");
            return usageChecker.GetCounterValue(elementId.Value, myUnityEditorUsageCounter);
        }

        protected override int GetOwnCount(SolutionAnalysisService swa, IGlobalUsageChecker usageChecker, IDeclaredElement element,
            ElementId? elementId)
        {
            Assertion.Assert(elementId.HasValue, "elementId.HasValue");
            return usageChecker.GetCounterValue(elementId.Value, myUnityEditorUsageCounter);
        }

        public override bool IsAvailableFor(IDeclaredElement declaredElement, ElementId? elementId)
        {
            if (!elementId.HasValue)
                return false;

            if (declaredElement is IMethod method)
            {
                var cache = method.GetSolution().GetComponent<UnityEventHandlerReferenceCache>();
                return cache.IsEventHandler(method);
            }

            var unityApi = declaredElement.GetSolution().GetComponent<UnityApi>();
            if (!unityApi.IsDescendantOfMonoBehaviour(declaredElement as ITypeElement))
                return false;
            
            return true;
        }

        public override string ProviderId => "Unity Assets Usage";
        
        protected override string FormatShort(IDeclaredElement elt, CodeVisionState state, int ownCount, int baseCount)
        {
            if (IsNotReady(state))
                return Noun(elt, 0);
            
            if (baseCount == 0)
                return $"No asset usages";
            
            return $"{ownCount} {Noun(elt, ownCount)}"; //always plural
        }

        protected override bool ShowZeroResult() => true;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)};
        protected override IconId IconId => InsightUnityIcons.InsightUnity.Id;
    }
}