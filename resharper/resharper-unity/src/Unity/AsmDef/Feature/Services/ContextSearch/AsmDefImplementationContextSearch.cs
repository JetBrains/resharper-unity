using System.Linq;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.ContextSearch
{
    [ShellFeaturePart]
    public class AsmDefImplementationContextSearch : ImplementationContextSearch
    {
        protected override bool IsAvailableInternal(IDataContext dataContext) => false;

        public override bool IsContextApplicable(IDataContext dataContext)
        {
            if (!ContextNavigationUtil.CheckDefaultApplicability<JsonNewLanguage>(dataContext))
                return false;
            var declaredElements = dataContext.GetData(PsiDataConstants.DECLARED_ELEMENTS);

            return declaredElements != null && declaredElements.All(x => x is IAsmDefDeclaredElement);
        }
    }
}