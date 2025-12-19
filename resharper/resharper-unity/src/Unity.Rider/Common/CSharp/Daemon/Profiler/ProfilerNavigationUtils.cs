using System;
using System.Linq;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE.StackTrace;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Nodes;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Parsers;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

public static class ProfilerNavigationUtils
{
    internal static void ParseAndNavigateToParent(ISolution solution, string parentQualifiedName, ILogger logger)
    {
        var stackTraceOptions = solution.GetComponent<StackTraceOptions>();
        var parser = new StackTraceParser(parentQualifiedName, solution,
            solution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

        try
        {
            //parse the qualified name of the parent and navigate to it
            var rootNode = parser.Parse(0, parentQualifiedName.Length);
            // Use a visitor to extract the first navigation occurrence (decouples from PSI details)
            var visitor = new NavigationOccurrenceVisitor();
            rootNode.Accept(visitor);
            var occurrence = visitor.Result;

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

    // Visitor that finds the first navigable occurrence in the parsed stack trace tree
    private sealed class NavigationOccurrenceVisitor : StackTraceVisitor
    {
        public IOccurrence Result { get; private set; }

        public override void VisitResolvedNode(IdentifierNode node)
        {
            if (Result != null)
                return;

            var resolveState = node.ResolveState;
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                Result = resolveState.MainCandidate?.GetNavigationDeclarations().FirstOrDefault() ?? Result;
            }
        }

        public override void VisitResolvedPath(PathNode node)
        {
        }

        public override void VisitText(TextNode node)
        {
        }

        public override void VisitParameter(ParameterNode node)
        {
        }

        public override void VisitCompositeNode(CompositeNode node)
        {
            if (node == null)
                return;

            foreach (var child in node.Nodes)
            {
                if (Result != null)
                    break;
                child.Accept(this);
            }
        }
    }
}