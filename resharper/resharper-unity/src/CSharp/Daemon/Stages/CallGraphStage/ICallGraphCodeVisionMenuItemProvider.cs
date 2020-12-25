using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public interface ICallGraphCodeVisionMenuItemProvider
    {
        [NotNull]
        [ItemNotNull]
        IEnumerable<BulbMenuItem> GetMenuItems([NotNull] IMethodDeclaration methodDeclaration, [NotNull] ITextControl textControl, IReadOnlyCallGraphContext context);
    }
}