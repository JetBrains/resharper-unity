using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Json.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Resources;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.ContextActions
{
    [ContextAction(
        Group = "Unity", ResourceType = typeof(Strings), NameResourceName = nameof(Strings.ConvertToNamedAssemblyDefinitionReference_Name), DescriptionResourceName = nameof(Strings.ConvertToNamedAssemblyDefinitionReference_Description))]
    public class ConvertToNamedReferenceContextAction : ScopedContextActionBase<IJsonNewLiteralExpression>
    {
        private readonly IJsonNewContextActionDataProvider myDataProvider;

        public ConvertToNamedReferenceContextAction(IJsonNewContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => Strings.ConvertToNamedReferenceContextAction_Text_To_named_reference;

        protected override IJsonNewLiteralExpression? TryCreateInfoFromDataProvider(IUserDataHolder cache)
        {
            var literalExpression = myDataProvider.GetSelectedTreeNode<IJsonNewLiteralExpression>();
            if (literalExpression == null || (!literalExpression.IsReferencesArrayEntry() &&
                                              !literalExpression.IsReferencePropertyValue()))
            {
                return null;
            }

            return literalExpression;
        }

        protected override bool IsAvailable(IJsonNewLiteralExpression literalExpression)
        {
            if (!AsmDefUtils.IsGuidReference(literalExpression.GetUnquotedText()))
                return false;

            var reference = literalExpression.FindReference<AsmDefNameReference>();
            return reference != null && reference.Resolve().ResolveErrorType == ResolveErrorType.OK;
        }

        protected override Action<ITextControl>? ExecutePsiTransaction(IJsonNewLiteralExpression literalExpression,
                                                                       ISolution solution,
                                                                       IProgressIndicator progress)
        {
            var reference = literalExpression.FindReference<AsmDefNameReference>();
            var declaredElement = reference?.Resolve().DeclaredElement;
            if (declaredElement != null)
            {
                using (WriteLockCookie.Create())
                {
                    var newLiteralExpression =
                        myDataProvider.ElementFactory.CreateStringLiteral(declaredElement.ShortName);
                    ModificationUtil.ReplaceChild(literalExpression, newLiteralExpression);
                }
            }
            return null;
        }
    }
}