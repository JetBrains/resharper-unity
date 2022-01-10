using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon
{
    // TODO: Rename to something like CSharpUnityElementProblemAnalyzer, and replace Analyze with Run
    public abstract class UnityElementProblemAnalyzer<T> : UnityElementProblemAnalyzerBase<T, CSharpLanguage>
        where T : ITreeNode
    {
        protected UnityElementProblemAnalyzer(UnityApi unityApi)
        {
            Api = unityApi;
        }

        protected UnityApi Api { get; }

        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
        {
            if (base.ShouldRun(file, data))
            {
                // All C# files should be part of a user project
                return data.SourceFile?.ToProjectFile()?.GetProject()?.IsProjectFromUserView() == true;
            }

            return false;
        }

        protected sealed override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}