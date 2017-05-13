using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.ERROR, UnityHighlightingGroupIds.Unity,
        OverlapResolve = OverlapResolveKind.ERROR,
        ToolTipFormatString = MESSAGE)]
    public class SyncVarUsageError : IHighlighting, IUnityHighlighting
    {
        private const string MESSAGE = "SyncVar can only be applied in a class deriving from NetworkBehaviour";

        private readonly IAttribute myAttribute;

        public SyncVarUsageError(IAttribute attribute)
        {
            myAttribute = attribute;
        }

        public bool IsValid() => myAttribute == null || myAttribute.IsValid();
        public DocumentRange CalculateRange() => myAttribute.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}