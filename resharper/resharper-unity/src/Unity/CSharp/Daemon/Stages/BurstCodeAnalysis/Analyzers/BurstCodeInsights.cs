using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
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
        
        [NotNull]
        [ItemNotNull]
        public IEnumerable<BulbMenuItem> GetBurstActions([NotNull] IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var result = new CompactList<BulbMenuItem>();
            var textControl = myTextControlManager.LastFocusedTextControl.Value;
            
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