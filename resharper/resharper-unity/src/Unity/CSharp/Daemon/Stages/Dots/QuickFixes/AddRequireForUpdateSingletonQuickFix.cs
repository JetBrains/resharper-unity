using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.QuickFixes
{
    [QuickFix]
    public class AddRequireForUpdateSingletonQuickFix : UnityScopedQuickFixBase
    {
        private readonly string myClassName;
        private readonly IClassLikeDeclaration myClassLikeDeclaration;

        public override string Text =>
            string.Format(Strings.UnityDots_Add_RequireForUpdate_SingletonQuickFix, myClassName);

        public override string ScopedText => Strings.UnityDots_Add_RequireForUpdate_SingletonQuickFix_For_All;

        public AddRequireForUpdateSingletonQuickFix(SingletonMustBeRequestedWarning warning)
        {
            myClassName = warning.RequestedTypeName;
            myClassLikeDeclaration = warning.ClassLikeDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var onCreateMethod = DotsUtils.GetMethodsFromAllDeclarations(myClassLikeDeclaration)
                    .FirstOrDefault(m => DotsUtils.IsISystemOnCreateMethod(m.DeclaredElement));
               
                if (onCreateMethod == null)
                {
                    var factory = CSharpElementFactory.GetInstance(myClassLikeDeclaration);

                    var updateMethod =
                        factory.CreateTypeMemberDeclaration(
                            "public void OnCreate(ref SystemState state){state.RequireForUpdate<$0>();}",
                            myClassName);
                    ModificationUtil.AddChildAfter(myClassLikeDeclaration.Body.FirstChild, updateMethod);
                }
                else
                {
                    var refStateParameterName = onCreateMethod.DeclaredElement.Parameters[0].ShortName;
                    var factory = CSharpElementFactory.GetInstance(myClassLikeDeclaration);

                    var updateQueryExpression = factory.CreateStatement("$0.RequireForUpdate<$1>();",
                        refStateParameterName, myClassName);

                    ModificationUtil.AddChildAfter(onCreateMethod.Body.FirstChild, updateQueryExpression);
                }
            }

            return null;
        }

        protected override ITreeNode TryGetContextTreeNode()
        {
            return myClassLikeDeclaration;
        }
    }
}