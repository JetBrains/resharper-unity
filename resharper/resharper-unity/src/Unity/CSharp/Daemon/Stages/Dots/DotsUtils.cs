using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots
{
    public static class DotsUtils
    {
        public static IEnumerable<IMethodDeclaration> GetMethodsFromAllDeclarations(
            IClassLikeDeclaration classLikeDeclaration, Func<IClassLikeDeclaration,bool> predicate = null)
        {
            return GetMethodsFromAllDeclarations(classLikeDeclaration.DeclaredElement, predicate);
        }

        public static IEnumerable<IMethodDeclaration> GetMethodsFromAllDeclarations(ITypeElement typeElement, Func<IClassLikeDeclaration,bool> predicate = null)
        {
            var sourceFiles = typeElement.GetSourceFiles();
            var result = new List<IMethodDeclaration>();
            foreach (IPsiSourceFile sourceFile in sourceFiles)
            {
                if(sourceFile.IsSourceGeneratedFile())
                    continue;

                var declarations = typeElement.GetDeclarationsIn(sourceFile);
                foreach (var declaration in declarations)
                {
                    if (declaration is IClassLikeDeclaration classLikeDeclaration
                        && (predicate?.Invoke(classLikeDeclaration) ?? true))
                    {
                        result.AddRange(classLikeDeclaration.MethodDeclarations);
                    }
                }
            }

            return result;
        }

        private static bool IsISystemMethod(IMethod method, string methodName)
        {
            if (method == null)
                return false;

            if (method.ShortName != methodName)
                return false;

            if (method.Parameters.Count != 1)
                return false;

            var methodParameter = method.Parameters[0];
            if (!UnityApi.IsSystemStateType(methodParameter.Type.GetTypeElement()))
                return false;

            if (!methodParameter.IsRefMember())
                return false;

            return true;
        }

        public static bool IsISystemOnCreateMethod(IMethod method)
        {
            return IsISystemMethod(method, "OnCreate");
        }

        public static bool IsISystemOnDestroyMethod(IMethod method)
        {
            return IsISystemMethod(method, "OnDestroy");
        }

        public static bool IsISystemOnUpdateMethod(IMethod method)
        {
            return IsISystemMethod(method, "OnUpdate");
        }
    }
}