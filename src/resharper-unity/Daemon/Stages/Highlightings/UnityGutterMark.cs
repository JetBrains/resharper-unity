using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.BulbMenu;
using JetBrains.Util;

[assembly: RegisterHighlighter(UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE, 
    EffectType = EffectType.GUTTER_MARK, GutterMarkType = typeof(UnityGutterMark), 
    Layer = HighlighterLayer.SYNTAX + 1)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
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
            var highlighting = daemon.GetHighlighting(highlighter) as UnityMarkOnGutter;
            if (highlighting != null)
                return highlighting.GetBulbMenuItems(solution, textControl);

            return EmptyList<BulbMenuItem>.InstanceList;
        }
    }
}