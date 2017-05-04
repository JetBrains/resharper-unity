using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(RedundantInitializeOnLoadAttributeWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity,
    "Redundant InitializeOnLoad attribute",
    "InitializeOnLoad attribute is redundant when static constructor is missing.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
        OverlapResolve = OverlapResolveKind.DEADCODE)]
    public class RedundantInitializeOnLoadAttributeWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.RedundantInitializeOnLoadAttribute";
        public const string MESSAGE = "InitializeOnLoad attribute is redundant when static constructor is missing";

        public RedundantInitializeOnLoadAttributeWarning(IAttribute attribute)
        {
            Attribute = attribute;
        }

        public IAttribute Attribute { get; }

        public bool IsValid() => Attribute == null || Attribute.IsValid();
        public DocumentRange CalculateRange() => Attribute.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}