using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.BulbMenu;
using JetBrains.Util;

[assembly: RegisterHighlighter(HighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE, EffectType = EffectType.GUTTER_MARK, GutterMarkType = typeof(UnityMessageGutterMark), Layer = HighlighterLayer.SYNTAX + 1)]

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon
{
    public class UnityMessageGutterMark : IconGutterMark
    {
        public UnityMessageGutterMark()
            : base(LogoThemedIcons.UnityLogo.Id)
        {
        }

        public override IAnchor Anchor => BulbMenuAnchors.PermanentBackgroundItems;

        public override IEnumerable<BulbMenuItem> GetBulbMenuItems(IHighlighter highlighter)
        {
            return EmptyList<BulbMenuItem>.InstanceList;
        }
    }
}