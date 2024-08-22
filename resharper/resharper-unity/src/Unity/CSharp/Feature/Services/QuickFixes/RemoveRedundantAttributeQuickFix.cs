using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.BulbActions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class RemoveRedundantAttributeQuickFix : ModernScopedNonIncrementalQuickFixBase
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

        public override string Text => Strings.RemoveRedundantAttributeQuickFix_Text_Remove_redundant_attribute;

        // We don't remove all redundant attributes, just Unity ones
        public override string ScopedText => Strings.RemoveRedundantAttributeQuickFix_ScopedText_Remove_redundant_Unity_attributes;

        public override bool IsReanalysisRequired => false;
        public override ITreeNode ReanalysisDependencyRoot => null;

        protected override ITreeNode TryGetContextTreeNode() => myAttribute;

        protected override IBulbActionCommand ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var attributeList = AttributeListNavigator.GetByAttribute(myAttribute);
            attributeList?.RemoveAttribute(myAttribute);
            return null;
        }
    }
}