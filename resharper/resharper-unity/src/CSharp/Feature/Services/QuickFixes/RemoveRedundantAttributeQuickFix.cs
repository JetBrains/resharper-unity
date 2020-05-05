using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class RemoveRedundantAttributeQuickFix : IQuickFix
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

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            return new RemoveAttribute(myAttribute).ToQuickFixIntentions();
        }

        private class RemoveAttribute : BulbActionBase
        {
            private readonly IAttribute myAttribute;

            public RemoveAttribute(IAttribute attribute)
            {
                myAttribute = attribute;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var attributeList = AttributeListNavigator.GetByAttribute(myAttribute);
                attributeList?.RemoveAttribute(myAttribute);
                return null;
            }

            public override string Text => "Remove redundant attribute";
        }

        public bool IsAvailable(IUserDataHolder cache) => myAttribute.IsValid();
    }
}