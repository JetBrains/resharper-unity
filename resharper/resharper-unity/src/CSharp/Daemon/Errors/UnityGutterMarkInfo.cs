using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors
{
    public partial class UnityGutterMarkInfo : ICustomAttributeIdHighlighting
    {
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE;
    }
}