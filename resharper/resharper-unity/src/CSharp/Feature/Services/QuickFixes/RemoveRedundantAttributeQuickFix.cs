using JetBrains.ReSharper.Feature.Services.CodeCleanup.HighlightingModule;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix, HighlightingCleanupItem]
    public class RemoveRedundantAttributeQuickFix : CSharpScopedRemoveRedundantCodeQuickFixBase
    {
        private readonly IAttribute myAttribute;

        public RemoveRedundantAttributeQuickFix(RedundantInitializeOnLoadAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        public RemoveRedundantAttributeQuickFix(RedundantSerializeFieldAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        public RemoveRedundantAttributeQuickFix(RedundantHideInInspectorAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        public RemoveRedundantAttributeQuickFix(RedundantAttributeOnTargetWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        public RemoveRedundantAttributeQuickFix(RedundantFormerlySerializedAsAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        protected override ITreeNode TryGetContextTreeNode() => myAttribute;
        public override string Text => "Remove redundant attribute";
        // We don't remove all redundant attributes, just Unity ones
        public override string ScopedText => "Remove redundant Unity attributes";
        public override bool IsReanalysisRequired => false;
        public override ITreeNode ReanalysisDependencyRoot => null;

        public override void Execute()
        {
            var attributeList = AttributeListNavigator.GetByAttribute(myAttribute);
            attributeList?.RemoveAttribute(myAttribute);
        }

    }
}