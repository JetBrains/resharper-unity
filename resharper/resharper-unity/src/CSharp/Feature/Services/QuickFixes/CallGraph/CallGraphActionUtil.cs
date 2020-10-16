using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph
{
    public static class CallGraphActionUtil
    {
        public static void AppendAttributeInTransaction(
            [NotNull] IMethodDeclaration methodDeclaration,
            [NotNull] AttributeValue[] fixedArguments,
            [NotNull] Pair<string, AttributeValue>[] namedArguments,
            [NotNull] IClrTypeName protagonistAttributeName, 
            [NotNull] string commandName)
        {
            var factory = CSharpElementFactory.GetInstance(methodDeclaration);
            var symbolCache = methodDeclaration.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(methodDeclaration.GetPsiModule(), true, true);
            var protagonistTypeElement = symbolScope.GetTypeElementByCLRName(protagonistAttributeName);

            if (protagonistTypeElement == null)
                return;

            var protagonistAttribute = factory.CreateAttribute(protagonistTypeElement, fixedArguments, namedArguments);
            var lastAttribute = methodDeclaration.Attributes.LastOrDefault();
            var sectionList = AttributeSectionListNavigator.GetByAttribute(lastAttribute);
            var transactions = methodDeclaration.GetPsiServices().Transactions;

            transactions.Execute(commandName, () =>
            {
                //CGTD overlook. writelock?
                using (WriteLockCookie.Create())
                {
                    if (sectionList != null)
                    {
                        var fakeClass = factory.CreateTypeMemberDeclaration("[$0]class C{}", protagonistAttribute);
                        var attributeSection = AttributeSectionNavigator.GetByAttribute(fakeClass.Attributes[0]).NotNull();
                        var lastSection = sectionList.Sections.Last();
                        
                        ModificationUtil.AddChildAfter(lastSection, attributeSection);
                    }
                    else
                        methodDeclaration.AddAttributeAfter(protagonistAttribute, null);
                }
            });
        }
    }
}