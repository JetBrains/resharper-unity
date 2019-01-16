using System.Drawing;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighter(UnityHighlightingAttributeIds.UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE,
    GroupId = UnityHighlightingGroupIds.Unity, EffectType = EffectType.TEXT, FontStyle = FontStyle.Bold,
    Layer = HighlighterLayer.SYNTAX + 1)]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO, "UnityGutterMarks", Languages = "CSHARP", OverlapResolve = OverlapResolveKind.NONE)]
    public class UnityImplicitlyUsedIdentifierHighlighting : ICustomAttributeIdHighlighting, IUnityHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public UnityImplicitlyUsedIdentifierHighlighting(DocumentRange documentRange)
        {
            myDocumentRange = documentRange;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myDocumentRange;

        public string ToolTip => null;
        public string ErrorStripeToolTip => null;
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE;
    }
}