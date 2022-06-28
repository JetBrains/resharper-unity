using System;
using System.Collections.Generic;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

#nullable enable

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
            var declaration = AttributesOwnerDeclarationNavigator.GetByAttribute(myAttribute)
                .FirstOrDefault(d =>
                    // We ignore constants - if we marked the type as serialisable, the constant declaration wouldn't
                    // suddenly become serialisable
                    d is IFieldDeclaration { IsStatic: false, IsReadonly: false } ||
                    (d is IPropertyDeclaration { IsAuto: true, IsStatic: false, IsReadonly: false } &&
                     myAttribute.Target == AttributeTarget.Field));

            if (declaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            if (ValidUtils.Valid(typeDeclaration)
                && typeDeclaration.DeclaredName != SharedImplUtil.MISSING_DECLARATION_NAME
                && !api.IsUnityType(typeDeclaration.DeclaredElement))
            {
                return new MakeSerializable(typeDeclaration).ToQuickFixIntentions();
            }

            return EmptyList<IntentionAction>.Enumerable;
        }

        public bool IsAvailable(IUserDataHolder cache) => ValidUtils.Valid(myAttribute);

        private class MakeSerializable : BulbActionBase
        {
            private readonly ICSharpTypeDeclaration myTypeDeclaration;

            public MakeSerializable(ICSharpTypeDeclaration typeDeclaration)
            {
                myTypeDeclaration = typeDeclaration;
            }

            protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                AttributeUtil.AddAttributeToSingleDeclaration(myTypeDeclaration,
                    PredefinedType.SERIALIZABLE_ATTRIBUTE_CLASS, myTypeDeclaration.GetPsiModule(),
                    CSharpElementFactory.GetInstance(myTypeDeclaration));

                return null;
            }

            public override string Text => $"Make type '{myTypeDeclaration.DeclaredName}' serializable";
        }
    }
}
