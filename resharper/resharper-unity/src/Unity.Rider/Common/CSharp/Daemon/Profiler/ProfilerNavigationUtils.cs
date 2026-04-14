#nullable enable
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
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

public static class ProfilerNavigationUtils
{
    internal static string? Navigate(ISolution solution, ProfilerNavigationRequest request, ILogger logger)
    {
        try
        {
            if (request.ProfilerMarkerName != null)
            {
                if (TryNavigateToProfilerMarker(solution, request.QualifiedName, request.ProfilerMarkerName, logger))
                    return null;

                // Fallback: navigate to the parent method if the marker was not found
                logger.Verbose($"BeginSample(\"{request.ProfilerMarkerName}\") not found in '{request.QualifiedName}', falling back to method navigation");
                if (TryNavigateToMethod(solution, request.QualifiedName, logger))
                    return null;
            }
            else
            {
                if (TryNavigateToMethod(solution, request.QualifiedName, logger))
                    return null;
            }

            return string.Format(Strings.ProfilerNavigation_NoDeclarationFound, request.QualifiedName);
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return string.Format(Strings.ProfilerNavigation_UnexpectedError, request.QualifiedName);
        }
    }

    private static NavigationOccurrenceVisitor? ResolveQualifiedName(ISolution solution, string qualifiedName, ILogger logger)
    {
        var stackTraceOptions = solution.GetComponent<StackTraceOptions>();
        var parser = new StackTraceParser(qualifiedName, solution,
            solution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

        var rootNode = parser.Parse(0, qualifiedName.Length);
        var visitor = new NavigationOccurrenceVisitor();
        rootNode.Accept(visitor);

        if (visitor.Result != null || visitor.DeclaredElement != null) 
            return visitor;
        
        logger.Verbose($"No occurrence found for '{qualifiedName}'");
        return null;

    }

    private static bool TryNavigateToMethod(ISolution solution, string qualifiedName, ILogger logger)
    {
        var visitor = ResolveQualifiedName(solution, qualifiedName, logger);
        if (visitor?.Result == null)
            return false;

        using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
        {
            visitor.Result.Navigate(solution, solution.GetComponent<IMainWindowPopupWindowContext>().Source, true);
        }

        return true;
    }

    private static bool TryNavigateToProfilerMarker(ISolution solution, string parentQualifiedName, string markerName, ILogger logger)
    {
        var visitor = ResolveQualifiedName(solution, parentQualifiedName, logger);
        if (visitor?.DeclaredElement == null)
            return false;

        using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
        {
            var declaration = visitor.DeclaredElement.GetDeclarations().FirstOrDefault();
            if (declaration is not ICSharpDeclaration csharpDeclaration) 
                return false;
            
            foreach (var expression in csharpDeclaration.Descendants<IInvocationExpression>())
            {
                if (!expression.IsProfilerBeginSampleMethod())
                    continue;

                var argument = expression.ArgumentsEnumerable.FirstOrDefault();
                var name = argument?.Value?.GetText().Trim('"');
                if (!string.Equals(name, markerName, StringComparison.Ordinal))
                    continue;
                
                var sourceFile = expression.GetSourceFile();
                if (sourceFile == null)
                    return false;

                sourceFile.Navigate(expression.GetNavigationRange().TextRange, true);
                return true;
            }
        }

        return false;
    }

    // Visitor that finds the first navigable occurrence in the parsed stack trace tree
    private sealed class NavigationOccurrenceVisitor
        : StackTraceVisitor
    {
        public IOccurrence? Result { get; private set; } 
        public IDeclaredElement? DeclaredElement { get; private set; } 

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

        public override void VisitCompositeNode(CompositeNode? node)
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