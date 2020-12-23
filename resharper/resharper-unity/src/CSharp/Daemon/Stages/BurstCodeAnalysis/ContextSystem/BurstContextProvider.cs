using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class BurstContextProvider : CallGraphContextProviderBase
    {
        private readonly IProperty<bool> myIsBurstEnabledProperty;

        public BurstContextProvider(Lifetime lifetime, IElementIdProvider elementIdProvider, IApplicationWideContextBoundSettingStore store,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider, BurstMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            myIsBurstEnabledProperty = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings key) => key.EnableBurstCodeHighlighting);
        }

        public override CallGraphContextTag ContextTag => CallGraphContextTag.BURST_CONTEXT;
        public override bool IsContextAvailable => myIsBurstEnabledProperty.Value;
        public override bool IsContextChangingNode(ITreeNode node) => IsBurstProhibitedNode(node) || base.IsContextChangingNode(node);
    }
}