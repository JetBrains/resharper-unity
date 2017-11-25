using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(RedundantSerializeFieldAttributeWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, RedundantSerializeFieldAttributeWarning.MESSAGE,
    "Unity will ignore the 'SerializeField' attribute if a field is also marked with the 'NonSerialized' attribute",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatString = MESSAGE)]
    public class RedundantSerializeFieldAttributeWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.RedundantSerializeFieldAttribute";
        public const string MESSAGE = "Redundant 'SerializeField' attribute";

        public RedundantSerializeFieldAttributeWarning(IAttribute attribute)
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