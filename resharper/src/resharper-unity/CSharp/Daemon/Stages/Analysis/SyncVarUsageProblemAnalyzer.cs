using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
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
            var type = element.TypeReference?.Resolve()?.DeclaredElement as ITypeElement;
            if (type != null && Equals(type.GetClrName(), KnownTypes.SyncVarAttribute))
            {
                var containingType = element.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;
                var networkBehaviour = TypeFactory.CreateTypeByCLRName(KnownTypes.NetworkBehaviour, element.PsiModule);
                if (containingType?.IsDescendantOf(networkBehaviour.GetTypeElement()) == false)
                {
                    consumer.AddHighlighting(new SyncVarUsageError(element));
                }
            }
        }
    }
}