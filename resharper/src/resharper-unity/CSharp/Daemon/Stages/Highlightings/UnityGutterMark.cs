using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.Util;

[assembly: RegisterHighlighter(UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE,
    EffectType = EffectType.GUTTER_MARK, GutterMarkType = typeof(UnityGutterMark),
    Layer = HighlighterLayer.SYNTAX + 1)]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // This class describes the UI of a highlight (gutter icon), while an IHighlighting
    // is an instance of a highlight at a specific location in a document. The IHighlighting
    // instance refers to this highlighter's attribute ID to wire up the UI
    public class UnityGutterMark : IconGutterMark
    {
        public UnityGutterMark()
            : base(LogoThemedIcons.UnityLogo.Id)
        {
        }

        public override IAnchor Anchor => BulbMenuAnchors.PermanentBackgroundItems;

        public override IEnumerable<BulbMenuItem> GetBulbMenuItems(IHighlighter highlighter)
        {
            var solution = Shell.Instance.GetComponent<SolutionsManager>().Solution;
            if (solution == null)
                return EmptyList<BulbMenuItem>.InstanceList;

            var textControlManager = solution.GetComponent<ITextControlManager>();
            var textControl = textControlManager.FocusedTextControl.Value;

            var daemon = solution.GetComponent<IDaemon>();
            if (daemon.GetHighlighting(highlighter) is UnityGutterMarkInfo highlighting)
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                    return highlighting.GetBulbMenuItems(solution, textControl);
            }

            return EmptyList<BulbMenuItem>.InstanceList;
        }
    }
}