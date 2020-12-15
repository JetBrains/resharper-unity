using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    public interface ICallGraphCodeVisionMenuItemProvider
    {
        [CanBeNull]
        BulbMenuItem GetMenuItem([NotNull] IMethodDeclaration methodDeclaration, [NotNull] ITextControl textControl, DaemonProcessKind processKind);
    }
}