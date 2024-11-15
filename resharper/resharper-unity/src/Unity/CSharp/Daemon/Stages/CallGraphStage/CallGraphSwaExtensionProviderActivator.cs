using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;

[SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
public class CallGraphSwaExtensionProviderActivator
{
    public CallGraphSwaExtensionProviderActivator(
        Lifetime lifetime,
        UnitySolutionTracker solutionTracker,
        ILazy<CallGraphSwaExtensionProvider> callGraphSwaExtensionProvider
    )
    {
        // solutionTracker.IsUnityProject.WhenTrue(lifetime, _ => callGraphSwaExtensionProvider.GetValueAsync(lifetime));
    }
}