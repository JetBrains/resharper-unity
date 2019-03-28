using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
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
    public class UnityGutterMark : AbstractUnityGutterMark
    {
        public UnityGutterMark()
            : base(UnityGutterIcons.UnityLogo.Id)
        {
        }
    }
}