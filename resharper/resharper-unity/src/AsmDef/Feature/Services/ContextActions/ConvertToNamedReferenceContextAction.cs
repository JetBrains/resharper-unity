using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.ContextActions
{
    [ContextAction(Name = "Convert to named assembly definition reference",
        Description = "Convert assembly definition reference from GUID to name based",
        Group = "Unity")]
    public class ConvertToNamedReferenceContextAction : ScopedContextActionBase<IJsonNewLiteralExpression>
    {
        private readonly IJsonNewContextActionDataProvider myDataProvider;

        public ConvertToNamedReferenceContextAction(IJsonNewContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public override string Text => "To named reference";

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
            if (!literalExpression.GetUnquotedText().StartsWith("guid:", StringComparison.InvariantCultureIgnoreCase))
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