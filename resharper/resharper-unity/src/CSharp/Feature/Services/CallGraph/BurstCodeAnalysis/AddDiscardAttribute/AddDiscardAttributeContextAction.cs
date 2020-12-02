using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.
    AddDiscardAttribute
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = AddDiscardAttributeUtil.DiscardActionMessage,
        Description = AddDiscardAttributeUtil.DiscardActionMessage,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddDiscardAttributeContextAction : BurstSimpleMethodContextAction
    {
        public AddDiscardAttributeContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected override IBulbAction GetBulbAction(IMethodDeclaration methodDeclaration)
        {
            return new AddDiscardAttributeBulbAction(methodDeclaration);
        }
    }
}