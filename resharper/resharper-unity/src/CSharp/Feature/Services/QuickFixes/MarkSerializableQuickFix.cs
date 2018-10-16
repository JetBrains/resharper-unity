using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class MarkSerializableQuickFix : IQuickFix
    {
        private readonly IAttribute myAttribute;

        public MarkSerializableQuickFix(RedundantSerializeFieldAttributeWarning highlighting)
        {
            myAttribute = highlighting.Attribute;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var api = myAttribute.GetSolution().GetComponent<UnityApi>();
            var typeDeclaration = myAttribute.GetContainingTypeDeclaration();
            var field = myAttribute.GetFieldsByAttribute().FirstOrDefault();

            if (field == null)
                return EmptyList<IntentionAction>.Enumerable;

            if (ValidUtils.Valid(typeDeclaration)
                && typeDeclaration.DeclaredName != SharedImplUtil.MISSING_DECLARATION_NAME
                && !api.IsUnityType(typeDeclaration.DeclaredElement)
                && CouldBeSerializedField(field))
            {
                return new MakeSerializable(typeDeclaration).ToQuickFixIntentions();
            }

            return EmptyList<IntentionAction>.Enumerable;
        }

        public bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myAttribute);

        private static bool CouldBeSerializedField(IField field)
        {
            return !field.IsStatic && !field.IsConstant && !field.IsReadonly;
        }

        private class MakeSerializable : BulbActionBase
        {
            private readonly ICSharpTypeDeclaration myTypeDeclaration;

            public MakeSerializable(ICSharpTypeDeclaration typeDeclaration)
            {
                myTypeDeclaration = typeDeclaration;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var attributeType = TypeFactory.CreateTypeByCLRName(PredefinedType.SERIALIZABLE_ATTRIBUTE_CLASS,
                    myTypeDeclaration.GetPsiModule()).GetTypeElement();
                if (attributeType != null)
                {
                    var elementFactory = CSharpElementFactory.GetInstance(myTypeDeclaration);
                    var attribute = elementFactory.CreateAttribute(attributeType);
                    myTypeDeclaration.AddAttributeAfter(attribute, null);
                }

                return null;
            }

            public override string Text => $"Make type '{myTypeDeclaration.DeclaredName}' serializable";
        }
    }
}