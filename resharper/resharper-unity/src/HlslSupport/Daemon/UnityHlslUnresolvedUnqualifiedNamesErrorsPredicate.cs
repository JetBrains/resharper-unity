using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Daemon
{
    [SolutionComponent]
    public class UnityHlslUnresolvedUnqualifiedNamesErrorsPredicate : IHlslUnresolvedUnqualifiedNamesErrorsPredicate {
        public bool SuppressUnresolvedErrors(CppFile cppFile, IContextBoundSettingsStore settingsStore)
        {
            return settingsStore.GetValue((UnitySettings k) => k.SuppressShaderErrorHighlighting);
        }
    }
}