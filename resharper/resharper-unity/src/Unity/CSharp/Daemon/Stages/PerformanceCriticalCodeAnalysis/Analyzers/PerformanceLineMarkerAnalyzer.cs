using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.ContainerAsyncAnyThread)]
    public class PerformanceLineMarkerAnalyzer : PerformanceProblemAnalyzerBase<IFunctionDeclaration>
    {
        private readonly IProperty<PerformanceHighlightingMode> myLineMarkerStatus;

        public PerformanceLineMarkerAnalyzer(Lifetime lifetime, IApplicationWideContextBoundSettingStore settingsStore, IThreading threading)
        {
            var apartmentForNotifications = ApartmentForNotifications.Primary(threading);
            myLineMarkerStatus = settingsStore.BoundSettingsStore
                .GetValueProperty2(lifetime, (UnitySettings key) => key.PerformanceHighlightingMode, apartmentForNotifications);
        }

        protected sealed override void Analyze(IFunctionDeclaration functionDeclaration,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (myLineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(
                    new UnityPerformanceCriticalCodeLineMarker(GetHighlightRange(functionDeclaration)));
            }
        }

        protected virtual DocumentRange GetHighlightRange(IFunctionDeclaration functionDeclaration)
        {
            // As always, ReSharper behaviour is the default, and we override with Rider. This makes code and testing
            // easier. We can avoid having Unity.Tests, Unity.Rider.Tests and Unity.VisualStudio.Tests
            return functionDeclaration.GetNameDocumentRange();
        }
    }
}