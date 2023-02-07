using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class GenerateBakerAndAuthoringActionFix : WorkflowDrivenImplementMembersFix<GenerateBakerAndAuthoringActionWorkflow>
    {
        [CanBeNull] private readonly ITreeNode myAnchor;

        public GenerateBakerAndAuthoringActionFix(IClassLikeDeclaration typeDeclaration, ITreeNode anchor = null)
            : base(typeDeclaration)
        {
            myAnchor = anchor;
        }

        public override string Text => Strings.UnityDots_GenerateBakerAndAuthoring_Name;

        protected override GenerateBakerAndAuthoringActionWorkflow TryCreateWorkflow()
        {
            return new GenerateBakerAndAuthoringActionWorkflow();
        }

        protected override void ConfigureContext(IGeneratorContext context)
        {
            // Don't call base. This will add all values into InputEvents which means all items are checked in the list.
            // This is useful for a Quick Fix that is implementing missing members, but we're more of a Context Action.
            // Try and set the anchor for where we want to generate the new members, if we have one
            if (context is CSharpGeneratorContext csharpContext)
                csharpContext.Anchor = myAnchor;
        }
    }
}