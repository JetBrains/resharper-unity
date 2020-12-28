using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public static class CallGraphActionUtil
    {
        public static IEnumerable<BulbMenuItem> ToMenuItems(this IEnumerable<ShowCallsBulbActionBase> bulbs, ITextControl textControl, ISolution solution)
        {
            return bulbs.Select(bulb => UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, solution, bulb.Icon));
        }
    }
}