using System;
using System.Linq;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE.StackTrace;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Nodes;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Parsers;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Psi;
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
        var stackTraceOptions = solution.GetComponent<StackTraceOptions>();
        var parentSample = sampleParent;
        if (parentSample == null) return;
        if(parentSample.IsProfilerMarker)
            parentSample = parentSample.Parent;
        var parentQualifiedName = parentSample.QualifiedName;
        var parser = new StackTraceParser(parentQualifiedName, solution,
            solution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

        try
        {
            //parse the qualified name of the parent and navigate to it
            var rootNode = parser.Parse(0, parentQualifiedName.Length);
            IOccurrence occurrence = null;

            foreach (var nodeNode in rootNode.Nodes)
            {
                if (nodeNode is IdentifierNode identifierNode)
                {
                    var identifierNodeResolveState = identifierNode.ResolveState;

                    using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                    {
                        occurrence = identifierNodeResolveState.MainCandidate?
                            .GetNavigationDeclarations().FirstOrDefault() ?? occurrence;
                    }
                }
            }

            if (occurrence != null)
                occurrence.Navigate(solution, solution.GetComponent<IMainWindowPopupWindowContext>().Source, true);
            else
                logger.Verbose($"No occurrence found for '{parentQualifiedName}'");
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }
}