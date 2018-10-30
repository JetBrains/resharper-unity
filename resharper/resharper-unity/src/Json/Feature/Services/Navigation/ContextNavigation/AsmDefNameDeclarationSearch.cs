using System;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.Navigation.ContextNavigation
{
    // Allows us to find the declaration from one of our IDeclaredElements, such as from ctrl+click/go to declaration on
    // a reference. This is required because our IDeclaredElement isn't backed by an IDeclaration (as it's not implemented
    // on IJavaScriptLiteralExpression). We have to return a search request that can find occurrence instances from an
    // IDeclaredElement. We return a simple Occurrence that uses the navigation detail we store in our declared element
    [ShellFeaturePart]
    public class AsmDefNameDeclarationSearch : DefaultDeclarationSearch
    {
        public override bool IsGotoDeclarationApplicable(IDeclaredElement declaredElement)
        {
            return declaredElement is AsmDefNameDeclaredElement;
        }

        public override bool IsContextApplicable(IDataContext dataContext)
        {
            var elements = dataContext.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            if (elements == null) return false;

            return elements.Any(element => element is AsmDefNameDeclaredElement);
        }

        protected override SearchDeclarationsRequest GetDeclarationSearchRequest(DeclaredElementTypeUsageInfo elementInfo, Func<bool> checkCancelled)
        {
            return new AsmDefNameSearchDeclarationRequest(elementInfo, checkCancelled);
        }
    }
}