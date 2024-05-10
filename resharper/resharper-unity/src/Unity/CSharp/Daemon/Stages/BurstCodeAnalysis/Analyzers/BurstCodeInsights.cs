using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    public class BurstCodeInsights
    {
        private readonly ITextControlManager myTextControlManager;
        private readonly IEnumerable<IBurstBulbItemsProvider> myBulbProviders;

        public BurstCodeInsights(
            ITextControlManager textControlManager,
            IEnumerable<IBurstBulbItemsProvider> bulbProviders)
        {
            myTextControlManager = textControlManager;
            myBulbProviders = bulbProviders;
        }

        public IEnumerable<BulbMenuItem> GetBurstActions(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var result = new CompactList<BulbMenuItem>();
            var textControl = myTextControlManager.LastFocusedTextControlPerClient.ForCurrentClient();
            if (textControl == null)
                return result;

            foreach (var bulbProvider in myBulbProviders)
            {
                var menuItems = bulbProvider.GetMenuItems(methodDeclaration, textControl, context);
                foreach(var item in menuItems)
                    result.Add(item);
            }

            return result;
        }
    }
}
