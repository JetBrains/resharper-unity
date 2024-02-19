using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class ShowMethodCallsBulbActionBase : ShowCallsBulbActionBase
    {
        private readonly DeclaredElementInstance<IDeclaredElement> myMethod;
        protected ShowMethodCallsBulbActionBase(IMethodDeclaration methodDeclaration, ShowCallsType callsType)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            
            Assertion.AssertNotNull(declaredElement, "declared is null, should be impossible");
            
            myMethod = new DeclaredElementInstance<IDeclaredElement>(declaredElement);
            CallsType = callsType;
        }

        protected override DeclaredElementInstance<IDeclaredElement> GetStartElement() => myMethod;

        protected override ShowCallsType CallsType { get; }
    }
}