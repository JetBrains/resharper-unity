using System;
using System.Linq;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE.StackTrace;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Navigation.NavigationExtensions;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Nodes;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Parsers;
using JetBrains.ReSharper.Plugins.Unity.CSharp;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

public static class ProfilerNavigationUtils
{
    internal static void Navigate(ISolution solution, ProfilerNavigationRequest request, ILogger logger)
    {
        if (request.ProfilerMarkerName != null)
            NavigateToProfilerMarker(solution, request.QualifiedName, request.ProfilerMarkerName, logger);
        else
            NavigateToMethod(solution, request.QualifiedName, logger);
    }

    internal static void ParseAndNavigateToParent(ISolution solution, string parentQualifiedName, ILogger logger)
    {
        NavigateToMethod(solution, parentQualifiedName, logger);
    }

    private static void NavigateToMethod(ISolution solution, string parentQualifiedName, ILogger logger)
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
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                {
                    occurrence.Navigate(solution, solution.GetComponent<IMainWindowPopupWindowContext>().Source, true);
                }
            }
            else
                logger.Verbose($"No occurrence found for '{parentQualifiedName}'");
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }

    private static void NavigateToProfilerMarker(ISolution solution, string parentQualifiedName, string markerName, ILogger logger)
    {
        var stackTraceOptions = solution.GetComponent<StackTraceOptions>();
        var parser = new StackTraceParser(parentQualifiedName, solution,
            solution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

        try
        {
            var rootNode = parser.Parse(0, parentQualifiedName.Length);
            var visitor = new NavigationOccurrenceVisitor();
            rootNode.Accept(visitor);
            var declaredElement = visitor.DeclaredElement;

            if (declaredElement == null)
            {
                logger.Verbose($"No declared element found for '{parentQualifiedName}'");
                return;
            }

            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                var declaration = declaredElement.GetDeclarations().FirstOrDefault();
                if (declaration is ICSharpDeclaration csharpDeclaration)
                {
                    foreach (var expression in csharpDeclaration.Descendants<IInvocationExpression>())
                    {
                        if (!expression.IsProfilerBeginSampleMethod())
                            continue;

                        var argument = expression.ArgumentsEnumerable.FirstOrDefault();
                        var name = argument?.Value?.GetText().Trim('"');
                        if (string.Equals(name, markerName, StringComparison.Ordinal))
                        {
                            expression.GetSourceFile()?.Navigate(expression.GetNavigationRange().TextRange, true);
                            return;
                        }
                    }
                }
            }

            // Fallback: navigate to the parent method if the marker was not found
            logger.Verbose($"BeginSample(\"{markerName}\") not found in '{parentQualifiedName}', falling back to method navigation");
            NavigateToMethod(solution, parentQualifiedName, logger);
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
        public IDeclaredElement DeclaredElement { get; private set; }

        public override void VisitResolvedNode(IdentifierNode node)
        {
            if (Result != null)
                return;

            var resolveState = node.ResolveState;
            using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
            {
                DeclaredElement = resolveState.MainCandidate ?? DeclaredElement;
                Result = DeclaredElement?.GetNavigationDeclarations().FirstOrDefault() ?? Result;
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