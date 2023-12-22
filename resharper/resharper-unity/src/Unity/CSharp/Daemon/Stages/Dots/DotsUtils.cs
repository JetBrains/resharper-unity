using System;
using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots
{
    public static class DotsUtils
    {
        public static IEnumerable<IMethodDeclaration> GetMethodsFromAllDeclarations(
            IClassLikeDeclaration classLikeDeclaration, Func<IClassLikeDeclaration, bool> predicate = null)
        {
            return GetMethodsFromAllDeclarations(classLikeDeclaration.DeclaredElement, predicate);
        }

        public static IEnumerable<IMethodDeclaration> GetMethodsFromAllDeclarations(ITypeElement typeElement, Func<IClassLikeDeclaration, bool> predicate = null)
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
            if (!methodParameter.Type.GetTypeElement().IsClrName(KnownTypes.SystemState))
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

        public static bool IsUnityProjectWithEntitiesPackage(IDataContext dataContext)
        {
            var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
            var project = dataContext.GetData(ProjectModelDataConstants.PROJECT);
            return HasEntitiesPackageInternal(solution, project);
        }
        
        public static bool IsUnityProjectWithEntitiesPackage(TemplateAcceptanceContext context)
        {
            return HasEntitiesPackageInternal(context.Solution, context.GetProject());
        }

        private static bool HasEntitiesPackageInternal(ISolution solution, IProject project)
        {
            if (solution == null || !solution.HasUnityReference())
                return false;

            if (project != null && !project.IsUnityProject())
                return false;

            return solution.HasEntitiesPackage();
        }

        public static bool HasEntitiesPackage(this ISolution solution)
        {
            var packageManager = solution.GetComponent<PackageManager>();
            return packageManager.HasPackage(PackageManager.UnityEntitiesPackageName);
        }

        public static bool IsUnityProjectWithEntitiesPackage(IFile file)
        {
            return HasEntitiesPackageInternal(file.GetSolution(), file.GetProject());
        }
    }
}