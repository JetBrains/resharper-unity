using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

[DaemonStage(Instantiation.ContainerAsyncAnyThreadSafe,
    StagesBefore = [typeof(GlobalFileStructureCollectorStage)],
    StagesAfter = [typeof(LanguageSpecificDaemonStage)],
    HighlightingTypes = [typeof(Process)])]
public class UnityProfilerDaemon : CSharpDaemonStageBase
{
    private readonly ILazy<IUnityProfilerSnapshotDataProvider> mySnapshotDataProvider;
    private readonly ISolution mySolution;
    private readonly UnityProfilerInsightProvider myCodeInsightProvider;
    private readonly ILogger myLogger;

    public UnityProfilerDaemon(ILazy<IUnityProfilerSnapshotDataProvider> snapshotDataProvider,
        ISolution solution,
        UnityProfilerInsightProvider codeInsightProvider,
        IconHost iconHost,
        ISettingsStore settingsStore,
        ILogger logger)
    {
        mySnapshotDataProvider = snapshotDataProvider;
        mySolution = solution;
        myCodeInsightProvider = codeInsightProvider;
        myLogger = logger;
    }

    protected override bool IsSupported(IPsiSourceFile sourceFile)
    {
        if (!mySolution.HasUnityReference())
            return false;

        var projectFile = sourceFile.ToProjectFile();
        var project = projectFile?.GetProject();
        if (project != null && !project.IsUnityProject() && !project.IsMiscFilesProject())
            return false;

        return sourceFile.IsLanguageSupported<CSharpLanguage>();
    }

    protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
        DaemonProcessKind processKind, ICSharpFile file)
    {
        return new Process(file, mySolution, process, settings, mySnapshotDataProvider, myCodeInsightProvider, myLogger);
    }

    [HighlightingSource(HighlightingTypes = [typeof(UnityProfilerInsightProvider)])]
    private class Process(
        ICSharpFile file,
        ISolution solution,
        IDaemonProcess process,
        IContextBoundSettingsStore settingsStore,
        ILazy<IUnityProfilerSnapshotDataProvider> snapshotDataProvider,
        UnityProfilerInsightProvider codeInsightProvider,
        ILogger logger)
        : IDaemonStageProcess
    {
        private static readonly HighlightingString ourMethodCallSingle =
            new(Strings.UnityProfilerSnapshot_Single_Method_Highlighting_display_name,
                Strings.UnityProfilerSnapshot_Single_Method_Highlighting_tooltip,
                Strings.UnityProfilerSnapshot_Single_Method_Highlighting_moreinfo);

        private static readonly HighlightingString ourMethodCallMultiple =
            new(Strings.UnityProfilerSnapshot_Multiple_Method_Highlighting_display_name,
                Strings.UnityProfilerSnapshot_Multiple_Method_Highlighting_tooltip,
                Strings.UnityProfilerSnapshot_Multiple_Method_Highlighting_moreinfo);

        private static readonly HighlightingString ourClassSingle =
            new(Strings.UnityProfilerSnapshot_Single_Class_Highlighting_display_name,
                Strings.UnityProfilerSnapshot_Single_Class_Highlighting_tooltip,
                Strings.UnityProfilerSnapshot_Single_Class_Highlighting_moreinfo);

        private static readonly HighlightingString ourClassMultiple =
            new(Strings.UnityProfilerSnapshot_Multiple_Class_Highlighting_display_name,
                Strings.UnityProfilerSnapshot_Multiple_Class_Highlighting_tooltip,
                Strings.UnityProfilerSnapshot_Multiple_Class_Highlighting_moreinfo);

        private static readonly HighlightingString ourInternalCall =
            new(Strings.UnityProfilerSnapshot_Internal_Call_Highlighting_display_name,
                Strings.UnityProfilerSnapshot_Internal_Call_Highlighting_tooltip,
                Strings.UnityProfilerSnapshot_Internal_Call_Highlighting_moreinfo);

        private readonly struct HighlightingString(string displayName, string tooltip, string moreText)
        {
            public readonly string DisplayName = displayName;
            public readonly string Tooltip = tooltip;
            public readonly string MoreText = moreText;
        }

        public void Execute(Action<DaemonStageResult> committer)
        {
            var textControl = solution.GetComponent<ITextControlManager>().LastFocusedTextControlPerClient
                .ForCurrentClient();

            var consumer = new FilteringHighlightingConsumer(DaemonProcess.SourceFile, file, settingsStore);
            using var samples = PooledList<PooledSample>.GetInstance();
            using var childrenSamples = PooledList<PooledSample>.GetInstance();

            foreach (var classLikeDeclaration in file.Descendants<IClassLikeDeclaration>())
            {
                IList<PooledSample> readOnlyList = samples;
                readOnlyList.Clear();

                var classCLRName = GetCLRName(classLikeDeclaration);
                snapshotDataProvider.Value.TryGetTypeSamples(classCLRName, ref readOnlyList);
                if (readOnlyList.Count == 0)
                    continue;

                CreateHighlightingForDeclaration(samples, textControl, consumer, classLikeDeclaration, codeInsightProvider, logger, solution, snapshotDataProvider);

                foreach (var descendant in classLikeDeclaration.Descendants<ICSharpDeclaration>())
                {
                    samples.Clear();
                    switch (descendant)
                    {
                        case IPropertyDeclaration propertyDeclaration:
                        {
                            foreach (var accessorDeclaration in propertyDeclaration.AccessorDeclarationsEnumerable)
                            {
                                using var accessorSamples = PooledList<PooledSample>.GetInstance();
                                IList<PooledSample> accessorListSamples = accessorSamples;
                                var clrName = GetCLRName(accessorDeclaration);
                                snapshotDataProvider.Value.TryGetSamplesByQualifiedName(clrName, ref accessorListSamples);

                                if (accessorListSamples.Count == 0)
                                    continue;

                                CreateHighlightingForDeclaration(accessorSamples, textControl, consumer,
                                    accessorDeclaration, codeInsightProvider, logger, solution, snapshotDataProvider);
                                ProcessInternalExpressions(childrenSamples, accessorListSamples, accessorDeclaration,
                                    consumer);

                                //collect samples from getters and setters for the property
                                samples.AddRange(accessorListSamples);
                            }

                            if (readOnlyList.Count == 0)
                                continue;

                            CreateHighlightingForDeclaration(samples, textControl, consumer, descendant, codeInsightProvider, logger, solution, snapshotDataProvider);
                            continue;
                        }
                        case IMethodDeclaration:
                        {
                            var clrName = GetCLRName(descendant);
                            if (!snapshotDataProvider.Value.TryGetSamplesByQualifiedName(clrName, ref readOnlyList))
                                continue;
                            if (readOnlyList.Count == 0)
                                continue;

                            CreateHighlightingForDeclaration(samples, textControl, consumer, descendant, codeInsightProvider, logger, solution, snapshotDataProvider);
                            ProcessInternalExpressions(childrenSamples, readOnlyList, descendant, consumer);
                            continue;
                        }
                    }
                }
            }

            committer(new DaemonStageResult(consumer.CollectHighlightings()));
        }

        private void ProcessInternalExpressions(PooledList<PooledSample> pooledChildrenSamples,
            IList<PooledSample> parentSamplesList, ICSharpDeclaration declaration,
            FilteringHighlightingConsumer consumer)
        {
            pooledChildrenSamples.Clear();
            foreach (var sample in parentSamplesList)
            {
                foreach (var child in sample.Children)
                {
                    pooledChildrenSamples.Add(child);
                    if (child.IsProfilerMarker)
                        pooledChildrenSamples.AddRange(child.Children);
                }
            }

            if (pooledChildrenSamples.Count == 0)
                return;

            //If this sample has children - go into the declaration and find invocations
            foreach (var sharpExpression in declaration.Descendants<ICSharpExpression>())
            {
                //process methods calls
                if (sharpExpression is IInvocationExpression invocationExpression)
                {
                    if (invocationExpression.IsProfilerBeginSampleMethod())
                    {
                        var beginSampleArgument = invocationExpression.ArgumentsEnumerable.FirstOrDefault();
                        var name = beginSampleArgument?.Value?.GetText().Trim('"');
                        ExtractSamplesAndAddHighlighting(pooledChildrenSamples, sharpExpression, declaration,
                            codeInsightProvider, consumer, name ?? string.Empty, ourInternalCall, snapshotDataProvider);
                        continue;
                    }

                    var (declaredElement, _, resolveErrorType) =
                        invocationExpression.InvocationExpressionReference.Resolve();

                    if (declaredElement != null && resolveErrorType == ResolveErrorType.OK)
                    {
                        ExtractHighlightingInformation(pooledChildrenSamples, declaredElement, sharpExpression,
                            declaration,
                            codeInsightProvider, consumer);
                    }

                    continue;
                }

                //process property calls
                if (sharpExpression is IReferenceExpression referenceExpression)
                {
                    var (declaredElement, _) = referenceExpression.Reference.Resolve();
                    if (declaredElement is IProperty property)
                    {
                        ExtractHighlightingInformation(pooledChildrenSamples, property.Getter, sharpExpression,
                            declaration, codeInsightProvider, consumer);
                        ExtractHighlightingInformation(pooledChildrenSamples, property.Setter, sharpExpression,
                            declaration, codeInsightProvider, consumer);
                    }
                }
            }
        }

        private static string GetCLRName(ICSharpDeclaration declaration)
        {
            if (declaration is IClassLikeDeclaration classLikeDeclaration)
                return classLikeDeclaration.CLRName;

            var cSharpTypeDeclaration = declaration.GetContainingTypeDeclaration()?.CLRName;
            var declarationDeclaredName = declaration.DeclaredName;
            var clrName = StringUtil.Combine(cSharpTypeDeclaration, declarationDeclaredName);
            return clrName;
        }

        private static void CreateHighlightingForDeclaration(IReadOnlyList<PooledSample> samples,
            ITextControl textControl,
            FilteringHighlightingConsumer consumer, ICSharpDeclaration declaration,
            UnityProfilerInsightProvider insightProvider, ILogger logger, ISolution solution,
            ILazy<IUnityProfilerSnapshotDataProvider> profilerSnapshotDataProvider)
        {
            var info = declaration is IClassLikeDeclaration
                ? CalculateDeclarationText(samples, ourClassSingle, ourClassMultiple)
                : CalculateDeclarationText(samples, ourMethodCallSingle, ourMethodCallMultiple);

            var bulbMenuItems = new List<BulbMenuItem>();
            using var pooledHashSet = PooledHashSet<PooledSample>.GetInstance();

            foreach (var s in samples)
            {
                if (s.Parent == null)
                    continue;
                pooledHashSet.Add(s.Parent);
            }

            List<ParentCalls> parents = null;
            if (!pooledHashSet.IsEmpty())
            {
                parents = new List<ParentCalls>();
                foreach (var sample in pooledHashSet)
                {
                    var bulb = new ShowProfilerCallsBulbAction(sample, logger);
                    var bulbMenuItem =
                        new IntentionAction(bulb, null, BulbMenuAnchors.FirstClassContextItems)
                            .ToBulbMenuItem(solution, textControl);

                    bulbMenuItems.Add(bulbMenuItem);


                    var realParentQualifiedName = sample.QualifiedName;
                    if (sample.IsProfilerMarker)
                        realParentQualifiedName = sample.Parent?.QualifiedName;
                        
                    parents.Add(new ParentCalls(sample.QualifiedName, sample.Duration, sample.FramePercentage, realParentQualifiedName));
                }
            }

            var durationSum = samples.Sum(s => s.Duration);
            var percentageSum = samples.Sum(s => s.FramePercentage);
            var memoryAllocation = samples.Sum(s => s.MemoryAllocation);

            var navigationRange = declaration.GetNavigationRange();

            var displaySettings = profilerSnapshotDataProvider.Value.GetGutterMarkSettings();

            var modelUnityProfilerSampleInfo = new ModelUnityProfilerSampleInfo(
                durationSum, 
                percentageSum,
                memoryAllocation, 
                info.Tooltip,
                parents, 
                displaySettings.ToProfilerGutterMarkRenderSettings());
            
            insightProvider.AddProfilerHighlighting(
                modelUnityProfilerSampleInfo,
                consumer, new DocumentRange(navigationRange.StartOffset, navigationRange.StartOffset + 1), declaration.DeclaredElement,
                info.DisplayName,
                info.Tooltip,
                info.MoreText,
                null, bulbMenuItems, new());
        }

        private readonly struct FunctionDeclarationSnapshotInfo(string displayName, string tooltip, string moreText)
        {
            public readonly string DisplayName = displayName;
            public readonly string Tooltip = tooltip;
            public readonly string MoreText = moreText;
        }

        private static FunctionDeclarationSnapshotInfo CalculateDeclarationText(IReadOnlyList<PooledSample> samples,
            HighlightingString single, HighlightingString multiple)
        {
            var totalDuration = samples.Sum(s => s.Duration);
            var totalPercentage = samples.Sum(s => s.FramePercentage);
            var totalAllocation = StringUtil.StrFormatByteSize(samples.Sum(s => s.MemoryAllocation));
            
            if (samples.Count == 1)
            {
                return new FunctionDeclarationSnapshotInfo(
                    string.Format(single.DisplayName, totalDuration, totalPercentage, totalAllocation),
                    string.Format(single.Tooltip, totalDuration, totalPercentage, totalAllocation),
                    string.Format(single.MoreText, totalDuration, totalPercentage, totalAllocation));
            }


            var min = samples.Min(s => s.Duration);
            var max = samples.Max(s => s.Duration);
            var avg = samples.Average(s => s.Duration);
            return new(
                string.Format(multiple.DisplayName, totalDuration, totalPercentage, samples.Count, totalAllocation),
                string.Format(multiple.Tooltip, totalDuration, totalPercentage, min, max, avg, totalAllocation, samples.Count),
                string.Format(multiple.MoreText, totalDuration, totalPercentage, min, max, avg, totalAllocation, samples.Count)
            );
        }

        private void ExtractHighlightingInformation(List<PooledSample> children,
            IDeclaredElement declaredElement, ICSharpExpression sharpExpression, ICSharpDeclaration declaration,
            UnityProfilerInsightProvider insightProvider, FilteringHighlightingConsumer consumer)
        {
            if (declaredElement is not IClrDeclaredElement clrDeclaredElement)
                return;
            var qualifiedName =
                $"{clrDeclaredElement.GetContainingType()?.GetClrName()}.{clrDeclaredElement.ShortName}";

            ExtractSamplesAndAddHighlighting(children, sharpExpression, declaration, insightProvider, consumer,
                qualifiedName, ourInternalCall, snapshotDataProvider);
        }

        private static void ExtractSamplesAndAddHighlighting(List<PooledSample> children,
            ICSharpExpression sharpExpression,
            ICSharpDeclaration declaration, UnityProfilerInsightProvider insightProvider,
            FilteringHighlightingConsumer consumer, string qualifiedName, HighlightingString highlightingString,
            ILazy<IUnityProfilerSnapshotDataProvider> snapshotDataProvider)
        {
            using var samples = PooledList<PooledSample>.GetInstance();
            foreach (var child in children)
            {
                if (child.QualifiedName.Equals(qualifiedName))
                   samples.Add(child); 
            }
            
            if (samples.Count == 0)
                return;
            var durationSum = samples.Sum(s => s.Duration);
            var percentageSum = samples.Sum(s => s.FramePercentage);
            var allocations = samples.Sum(s => s.MemoryAllocation);
            var memory = StringUtil.StrFormatByteSize(allocations);

            var displayName = string.Format(highlightingString.DisplayName, durationSum, percentageSum, samples.Count, memory);
            var tooltip = string.Format(highlightingString.Tooltip, qualifiedName, durationSum, percentageSum, memory,
                samples.Count);
            var moreText = string.Format(highlightingString.MoreText, qualifiedName, durationSum, percentageSum, memory,
                samples.Count);

            var renderSettings = snapshotDataProvider.Value.GetGutterMarkSettings().ToProfilerGutterMarkRenderSettings();
            var profilerSampleInfo =
                new ModelUnityProfilerSampleInfo(durationSum, percentageSum, allocations, tooltip, null, renderSettings);
            insightProvider.AddProfilerHighlighting(
                profilerSampleInfo
                , consumer, sharpExpression.GetDocumentRange(), declaration.DeclaredElement,
                displayName, tooltip, moreText, null
                , EmptyList<BulbMenuItem>.Instance, []);
        }

        public IDaemonProcess DaemonProcess { get; } = process;
    }
}