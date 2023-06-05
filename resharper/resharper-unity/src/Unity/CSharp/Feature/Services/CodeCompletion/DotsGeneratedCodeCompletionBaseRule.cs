using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    public abstract class DotsGeneratedCodeCompletionBaseRule : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            var completionType = context.BasicContext.CodeCompletionType;
            if (completionType != CodeCompletionType.BasicCompletion
                && completionType != CodeCompletionType.SmartCompletion)
                return false;

            if (context.PsiModule is not IProjectPsiModule projectPsiModule
                || !projectPsiModule.Project.IsUnityProject())
                return false;

            if (!projectPsiModule.GetSolution().HasEntitiesPackage())
                return false;
            
            return true;
        }
    }
}