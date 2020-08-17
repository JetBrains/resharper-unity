using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Daemon
{
    [SolutionComponent]
    public class UnityHlslAllErrorsPredicate : IHlslUnresolvedUnqualifiedNamesErrorsPredicate {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnityHlslAllErrorsPredicate(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }
        
        public bool SuppressUnresolvedErrors(CppFile cppFile, IContextBoundSettingsStore settingsStore)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return false;
            
            return settingsStore.GetValue((UnitySettings k) => k.SuppressShaderErrorHighlighting);
        }
        
        public bool SuppressUnresolvedUnqualifiedErrors(CppFile cppFile, IContextBoundSettingsStore settingsStore)
        {
            return SuppressUnresolvedErrors(cppFile, settingsStore);
        }
    }
}