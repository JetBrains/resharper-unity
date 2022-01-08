using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Rider.Backend.Features.RunMarkers;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Feature.RunMarkers
{
    [StaticSeverityHighlighting(Severity.INFO, typeof(RunMarkers), OverlapResolve = OverlapResolveKind.NONE)]
    public class UnityRunMarkerHighlighting : RunMarkerHighlighting
    {
        public UnityRunMarkerHighlighting([NotNull] IMethodDeclaration method, [NotNull] string attributeId,
            DocumentRange range, TargetFrameworkId targetFrameworkId)
            : base(method, attributeId, range, targetFrameworkId)
        {
        }
    }
}