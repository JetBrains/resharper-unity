using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IAttribute), HighlightingTypes = new[] { typeof(SyncVarUsageError) })]
    public class SyncVarUsageProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        public SyncVarUsageProblemAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (element.TypeReference?.Resolve().DeclaredElement is ITypeElement type &&
                Equals(type.GetClrName(), KnownTypes.SyncVarAttribute))
            {
                var containingType = element.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;
                if (!containingType.DerivesFrom(KnownTypes.NetworkBehaviour))
                    consumer.AddHighlighting(new SyncVarUsageError(element));
            }
        }
    }
}