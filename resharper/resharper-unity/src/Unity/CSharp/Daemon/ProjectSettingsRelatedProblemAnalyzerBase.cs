using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon
{
    public abstract class ProjectSettingsRelatedProblemAnalyzerBase<T> : UnityElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected ProjectSettingsRelatedProblemAnalyzerBase(UnityApi unityApi, UnityProjectSettingsCache projectSettingsCache)
            : base(unityApi)
        {
            ProjectSettingsCache = projectSettingsCache;
        }

        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) =>
            base.ShouldRun(file, data) && ProjectSettingsCache.IsAvailable();

        protected UnityProjectSettingsCache ProjectSettingsCache { get; }
    }
}