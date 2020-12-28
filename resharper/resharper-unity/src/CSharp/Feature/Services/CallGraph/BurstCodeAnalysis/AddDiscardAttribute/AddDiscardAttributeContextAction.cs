using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
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
    public sealed class AddDiscardAttributeContextAction : BurstContextActionBase
    {
        private readonly BurstGutterMarkProvider myBurstGutterMarkProvider;

        public AddDiscardAttributeContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myBurstGutterMarkProvider = dataProvider.Solution.GetComponent<BurstGutterMarkProvider>();
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new AddDiscardAttributeBulbAction(containingMethod).ToContextActionIntentions();
        }

        protected override bool IsAvailable(IUserDataHolder holder, IMethodDeclaration methodDeclaration)
        {
            var @base = base.IsAvailable(holder, methodDeclaration);

            return @base && !myBurstGutterMarkProvider.IsBurstGutterIconsEnabled;
        }
    }
}