#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[]
    {
        typeof(InheritorMustBeMarkedPartialReadonlyWarning)
    })]
    public class DotsPartialClassesAnalyzer : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
        {
            return DotsUtils.IsUnityProjectWithEntitiesPackage(file)
                   && base.ShouldRun(file, data);
        }

        public DotsPartialClassesAnalyzer(UnityApi unityApi) : base(unityApi)
        {
        }

        protected override void Analyze(IClassLikeDeclaration classLikeDeclaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var typeElement = classLikeDeclaration.DeclaredElement;
            var isDerivesFromIAspect = UnityApi.IsDerivesFromIAspect(typeElement);
            var isDerivesFromISystem = UnityApi.IsDerivesFromISystem(typeElement) || UnityApi.IsDerivesFromSystemBase(typeElement);

            if (!isDerivesFromISystem && !isDerivesFromIAspect)
                return;

            if (isDerivesFromIAspect)
            {
                var isReadonly = classLikeDeclaration.IsReadonly;
                var isPartial = classLikeDeclaration.IsPartial;

                if (isReadonly && isPartial)
                    return;

                consumer.AddHighlighting(new InheritorMustBeMarkedPartialReadonlyWarning(classLikeDeclaration, !isPartial, !isReadonly));
            }
            else if (isDerivesFromISystem)
            {
                var isPartial = classLikeDeclaration.IsPartial;

                if (!isPartial)
                    consumer.AddHighlighting(new InheritorMustBeMarkedPartialReadonlyWarning(classLikeDeclaration, !isPartial, false));
            }
        }
    }
}