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
using JetBrains.UI.Icons;
using JetBrains.Util;


namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // This class describes the UI of a highlight (gutter icon), while an IHighlighting
    // is an instance of a highlight at a specific location in a document. The IHighlighting
    // instance refers to this highlighter's attribute ID to wire up the UI
    public class AbstractUnityGutterMark : IconGutterMark
    {
        public AbstractUnityGutterMark(IconId id) : base(id)
        {
        }

        public override IAnchor Anchor => BulbMenuAnchors.PermanentBackgroundItems;

        public override IEnumerable<BulbMenuItem> GetBulbMenuItems(IHighlighter highlighter)
        {
            var solution = Shell.Instance.GetComponent<SolutionsManager>().Solution;
            if (solution == null)
                return EmptyList<BulbMenuItem>.InstanceList;
            
            var daemon = solution.GetComponent<IDaemon>();
            var highlighting = daemon.GetHighlighting(highlighter);

            if (highlighting != null)
            {
                var items = (highlighting as UnityGutterMarkInfo)?.Actions ??
                            (highlighting as UnityHotGutterMarkInfo)?.Actions;
                if (items != null)
                    return items;
            }
            return EmptyList<BulbMenuItem>.InstanceList;
        }
    }
}