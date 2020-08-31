using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Daemon
{
    [SolutionComponent]
    public class UnityHlslAllErrorsPredicate : IHlslUnresolvedUnqualifiedNamesErrorsPredicate {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly IProperty<bool> mySuppressShaderErrors;
        private readonly IProperty<bool> mySuppressShaderErrorsInRenderPipeline;

        public UnityHlslAllErrorsPredicate(Lifetime lifetime, ISolution solution, UnitySolutionTracker unitySolutionTracker, ISettingsStore settingsStore)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            mySuppressShaderErrors = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
                .GetValueProperty(lifetime, (UnitySettings key) => key.SuppressShaderErrorHighlighting);
            mySuppressShaderErrorsInRenderPipeline = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
                .GetValueProperty(lifetime, (UnitySettings key) => key.SuppressShaderErrorHighlightingInRenderPipelinePackages);
        }
        
        public bool SuppressUnresolvedErrors(CppFile cppFile, IContextBoundSettingsStore settingsStore)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return false;
            
            if (mySuppressShaderErrorsInRenderPipeline.Value)
            {
                var location = cppFile.File;
                if (location.FullPath.Contains(".render-pipelines"))
                    return true;
            }
            return mySuppressShaderErrors.Value;
        }
        
        public bool SuppressUnresolvedUnqualifiedErrors(CppFile cppFile, IContextBoundSettingsStore settingsStore)
        {
            return SuppressUnresolvedErrors(cppFile, settingsStore);
        }
    }
}