using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph
{
    public static class CallGraphActionUtil
    {
        public static readonly IClrTypeName ExpensiveMethodAttribute =
            new ClrTypeName("ExpensiveMethodAttribute");

        public static void AppendAttributeInTransaction([NotNull] IMethodDeclaration methodDeclaration,
            [NotNull] IClrTypeName protagonistAttributeName, [CanBeNull] IClrTypeName antagonistAttributeName,
            string commandName)
        {
            var factory = CSharpElementFactory.GetInstance(methodDeclaration);
            var symbolCache = methodDeclaration.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(methodDeclaration.GetPsiModule(), true, true);

            var protagonistTypeElement = symbolScope.GetTypeElementByCLRName(protagonistAttributeName);
            if (protagonistTypeElement == null) return;

            var protagonistAttribute = factory.CreateAttribute(protagonistTypeElement);
            IAttribute antagonistAttribute = null;
            if (antagonistAttributeName != null)
            {
                var antagonistTypeElement = symbolScope.GetTypeElementByCLRName(antagonistAttributeName);

                if (antagonistTypeElement != null)
                    antagonistAttribute = factory.CreateAttribute(antagonistTypeElement);
            }

            var transactions = methodDeclaration.GetPsiServices().Transactions;

            transactions.Execute(commandName, () =>
            {
                using (WriteLockCookie.Create())
                {
                    var lastAttribute = methodDeclaration.Attributes.LastOrDefault();
                    var sectionList = AttributeSectionListNavigator.GetByAttribute(lastAttribute);
                    if (sectionList != null)
                    {
                        var fakeClass = factory.CreateTypeMemberDeclaration("[$0]class C{}", protagonistAttribute);
                        var attributeSection =
                            AttributeSectionNavigator.GetByAttribute(fakeClass.Attributes[0]).NotNull();
                        var lastSection = sectionList.Sections.Last();
                        ModificationUtil.AddChildAfter(lastSection, attributeSection);
                    }
                    else
                    {
                        methodDeclaration.AddAttributeAfter(protagonistAttribute, null);
                    }

                    if (antagonistAttribute != null)
                        methodDeclaration.RemoveAttribute(antagonistAttribute);
                }
            });
        }
    }
}