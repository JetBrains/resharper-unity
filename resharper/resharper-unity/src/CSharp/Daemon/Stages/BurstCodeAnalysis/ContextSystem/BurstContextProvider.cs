using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
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

        public override bool IsMarkedLocal(IDeclaredElement declaredElement, CallGraphDataElement dataElement)
        {
            // there is problem:
            // due to root-ban check at not-merged state we have 2 data for graph - local data and index
            // they checks marks independently on their data
            // and the result on not-merged and merged state MAY DIFFER, for e.x. index has strict ban while data not
            // !!BUT!!!
            // cuz strictly banned at only at function declaration, it is impossible for index data to differ from local data very hard
            // only partial functions may result in false positives
            // in burst there are too little partial root(job) functions, performance-expensive same, they are mono behaviour hard coded
            // so it is mostly ok 
            
            if (declaredElement == null || dataElement == null)
                return false;

            var vertex = myElementIdProvider.GetElementId(declaredElement);

            if (vertex == null)
                return false;

            if (!dataElement.Vertices.Contains(vertex.Value) || dataElement.BanMarks.GetOrEmpty(BurstStrictlyBannedMarkProvider.RootMarkId).Contains(vertex.Value))
                return false;
                
            if (dataElement.RootMarks.GetOrEmpty(MarkId).Contains(vertex.Value))
                return true;

            return IsMarkedInternal(declaredElement, shouldPropagate:false, vertex);
        }
    }
}