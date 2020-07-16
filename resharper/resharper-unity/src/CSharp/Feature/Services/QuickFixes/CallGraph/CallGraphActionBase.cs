using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph
{
    public abstract class CallGraphActionBase : IBulbAction, IContextAction, IQuickFix
    {
        [CanBeNull] protected abstract IMethodDeclaration MethodDeclaration { get; }

        [NotNull] protected abstract IClrTypeName ProtagonistAttribute { get; }

        [CanBeNull] protected abstract IClrTypeName AntagonistAttribute { get; }

        public void Execute(ISolution solution, ITextControl textControl)
        {
            if (MethodDeclaration == null)
                return;

            CallGraphActionUtil.AppendAttributeInTransaction(
                MethodDeclaration,
                ProtagonistAttribute,
                AntagonistAttribute,
                GetType().Name);
        }

        public abstract string Text { get; }

        public abstract IEnumerable<IntentionAction> CreateBulbItems();

        public abstract bool IsAvailable(IUserDataHolder cache);
    }
}