using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Interfaces;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming
{
    [NamingConsistencyChecker(typeof(CSharpLanguage))]
    public class NamingConsistencyWarningSuppressor : INamingConsistencyChecker
    {
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            var project = sourceFile.GetProject();
            return project != null && project.IsUnityProject();
        }

        public void Check(IDeclaration declaration, INamingPolicyProvider namingPolicyProvider, out bool isFinalResult, out NamingConsistencyCheckResult result)
        {
            isFinalResult = false;

            var methodDeclaration = declaration as IMethodDeclaration;
            var method = methodDeclaration?.DeclaredElement;

            if (method != null)
            {
                var unityApi = method.GetSolution().GetComponent<UnityApi>();
                isFinalResult = unityApi.IsEventFunction(method);
            }

            result = isFinalResult ? NamingConsistencyCheckResult.OK : null;
        }
    }
}