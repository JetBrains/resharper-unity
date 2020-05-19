using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // This class describes the UI of a highlight (gutter icon), while an IHighlighting
    // is an instance of a highlight at a specific location in a document. The IHighlighting
    // instance refers to this highlighter's attribute ID to wire up the UI
    public class UnityGutterMark : AbstractUnityGutterMark
    {
        public UnityGutterMark()
            : base(UnityGutterIcons.UnityLogo.Id)
        {
        }
    }
}