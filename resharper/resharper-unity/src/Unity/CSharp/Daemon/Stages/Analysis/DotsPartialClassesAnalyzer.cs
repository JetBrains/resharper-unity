#nullable enable
using JetBrains.Metadata.Reader.Impl;
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
        typeof(InconsistentModifiersForDotsInheritorReadonlyWarning)
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
            var parentTypeName = EmptyClrTypeName.Instance;
            var shouldBeProcessed = false;
            var mustBeReadonly = false;

            if (UnityApi.IsDerivesFromIAspect(typeElement))
            {
                mustBeReadonly = !classLikeDeclaration.IsReadonly;
                shouldBeProcessed = true;
                parentTypeName = KnownTypes.IAspect;
            }
            else if (UnityApi.IsDerivesFromISystem(typeElement))
            {
                shouldBeProcessed = true;
                parentTypeName = KnownTypes.ISystem;
            }
            else if (UnityApi.IsDerivesFromSystemBase(typeElement))
            {
                shouldBeProcessed = true;
                parentTypeName = KnownTypes.SystemBase;
            }

            if (!shouldBeProcessed)
                return;

            var mustBePartial = !classLikeDeclaration.IsPartial;

            if (!mustBeReadonly && !mustBePartial)
                return;

            consumer.AddHighlighting(new InconsistentModifiersForDotsInheritorReadonlyWarning(classLikeDeclaration, parentTypeName.ShortName, mustBePartial, mustBeReadonly));
           
        }
    }
}