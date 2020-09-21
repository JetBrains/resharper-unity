using JetBrains.Annotations;
using JetBrains.Collections;
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
        public static void AppendAttributeInTransaction([NotNull] IMethodDeclaration methodDeclaration, CompactList<AttributeValue> compactList,
            [NotNull] IClrTypeName protagonistAttributeName, string commandName)
        {
            var factory = CSharpElementFactory.GetInstance(methodDeclaration);
            var symbolCache = methodDeclaration.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(methodDeclaration.GetPsiModule(), true, true);
            var protagonistTypeElement = symbolScope.GetTypeElementByCLRName(protagonistAttributeName);

            if (protagonistTypeElement == null)
            {
                Assertion.Fail($"{protagonistTypeElement} does not exist");
                return;
            }
            
            var protagonistAttribute = factory.CreateAttribute(protagonistTypeElement, compactList.ToArray(),
                EmptyArray<Pair<string, AttributeValue>>.Instance);
            var lastAttribute = methodDeclaration.Attributes.LastOrDefault();
            var sectionList = AttributeSectionListNavigator.GetByAttribute(lastAttribute);
            var transactions = methodDeclaration.GetPsiServices().Transactions;

            transactions.Execute(commandName, () =>
            {
                //CGTD writelock? Ask Vova
                using (WriteLockCookie.Create())
                {
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
                }
            });
        }
    }
}