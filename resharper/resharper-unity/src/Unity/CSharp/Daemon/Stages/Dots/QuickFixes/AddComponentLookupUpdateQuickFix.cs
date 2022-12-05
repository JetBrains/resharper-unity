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
    public class AddComponentLookupUpdateQuickFix : UnityScopedQuickFixBase
    {
        private readonly string myFieldShortName;
        private readonly IMethodDeclaration myOnUpdateMethod;
        private readonly IFieldDeclaration myFieldDeclaration;
        private readonly IClassLikeDeclaration myClassLikeDeclaration;

        public AddComponentLookupUpdateQuickFix(NotUpdatedComponentLookupWarning componentLookupWarning)
        {
            myFieldShortName = componentLookupWarning.ComponentLookupName;
            myOnUpdateMethod = componentLookupWarning.AvailableUpdateMethod;
            myFieldDeclaration = componentLookupWarning.QueryLookupFieldDeclaration;
            myClassLikeDeclaration = componentLookupWarning.ClassLikeDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            using (WriteLockCookie.Create())
            {
                var onUpdateMethod = myOnUpdateMethod;
                if (onUpdateMethod == null)
                {
                    var classLikeDeclaration = myClassLikeDeclaration;
                    onUpdateMethod = DotsUtils.GetMethodsFromAllDeclarations(classLikeDeclaration)
                        .FirstOrDefault(m => DotsUtils.IsISystemOnUpdateMethod(m.DeclaredElement));
                }

                if (onUpdateMethod == null)
                {
                    var factory = CSharpElementFactory.GetInstance(myClassLikeDeclaration);

                    var updateMethod =
                        factory.CreateTypeMemberDeclaration(
                            "public void OnUpdate(ref SystemState state){$0.Update(ref state);}",
                            myFieldShortName);
                    ModificationUtil.AddChildAfter(myClassLikeDeclaration.Body.FirstChild, updateMethod);
                }
                else
                {
                    var refStateParameterName = onUpdateMethod.DeclaredElement.Parameters[0].ShortName;
                    var factory = CSharpElementFactory.GetInstance(myFieldDeclaration);

                    var updateQueryExpression = factory.CreateStatement("$0.Update(ref $1);",
                        myFieldShortName, refStateParameterName);

                    ModificationUtil.AddChildAfter(onUpdateMethod.Body.FirstChild, updateQueryExpression);
                }
            }

            return null;
        }

        public override string Text =>
            string.Format(Strings.UnityDots_AddComponentLookup_Update_Text, myFieldShortName);

        public override string ScopedText => Strings.UnityDots_AddComponentLookupScoped_Update_Text;

        protected override ITreeNode TryGetContextTreeNode()
        {
            return myClassLikeDeclaration;
        }
    }
}