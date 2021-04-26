using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.ContextSearch
{
    public abstract class AsmDefImplementationContextSearchBase<TLanguage> : ImplementationContextSearch where TLanguage : PsiLanguageType
    {
        protected override bool IsAvailableInternal(IDataContext dataContext)
        {
            return false;
        }

        public override bool IsContextApplicable(IDataContext dataContext)
        {
            if (!ContextNavigationUtil.CheckDefaultApplicability<TLanguage>(dataContext))
                return false;
            var declaredElements = dataContext.GetData(PsiDataConstants.DECLARED_ELEMENTS);

            return declaredElements != null && declaredElements.All(x => x is IAsmDefDeclaredElement);
        }
    }
}