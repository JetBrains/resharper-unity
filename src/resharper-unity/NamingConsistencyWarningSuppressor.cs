using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Interfaces;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [NamingConsistencyChecker(typeof(CSharpLanguage))]
    public class NamingConsistencyWarningSuppressor : INamingConsistencyChecker
    {
        public bool IsApplicable(IPsiSourceFile sourceFile) => true;

        public void Check(IDeclaration declaration, INamingPolicyProvider namingPolicyProvider, out bool isFinalResult, out NamingConsistencyCheckResult result)
        {
            
            var methodDeclaration = declaration as IMethodDeclaration;
            if (methodDeclaration != null && MonoBehaviourUtil.IsEventHandler(methodDeclaration.DeclaredName))
            {
                var containingTypeElement = methodDeclaration.GetContainingTypeDeclaration().DeclaredElement;
                if (containingTypeElement != null && MonoBehaviourUtil.IsMonoBehaviourType(containingTypeElement, methodDeclaration.GetPsiModule()))
                {
                    result = NamingConsistencyCheckResult.OK;
                    isFinalResult = true;
                    return;
                }
            }

            result = null;
            isFinalResult = false;
        }
    }
}