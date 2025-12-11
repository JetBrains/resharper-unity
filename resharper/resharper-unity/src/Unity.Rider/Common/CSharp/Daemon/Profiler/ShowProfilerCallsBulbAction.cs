using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

public class ShowProfilerCallsBulbAction(PooledSample sampleParent, ILogger logger) : IBulbAction
{
    public string Text => string.Format(Strings.UnityProfilerSnapshot_Parent_Sample_Information,
        sampleParent.QualifiedName,
        sampleParent.Duration, 
        sampleParent.FramePercentage); 

    public void Execute(ISolution solution, ITextControl textControl)
    {
        var parentSample = sampleParent;
        if (parentSample == null) 
            return;
        
        if(parentSample.IsProfilerMarker) //get the parent marker in the case of Begin/EndSample
            parentSample = parentSample.Parent;
        
        if (parentSample == null) 
            return;
        
        var parentQualifiedName = parentSample.QualifiedName;
        ProfilerNavigationUtils.ParseAndNavigateToParent(solution, parentQualifiedName, logger);
    }
}