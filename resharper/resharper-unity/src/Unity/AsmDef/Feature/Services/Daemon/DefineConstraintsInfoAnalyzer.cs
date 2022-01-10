using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    // Note that problem analysers for NonUserCode will still show Severity.INFO
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
        HighlightingTypes = new[] { typeof(UnmetDefineConstraintInfo) })]
    public class DefineConstraintsInfoAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        private readonly PreProcessingDirectiveCache myPreProcessingDirectiveCache;
        private readonly UnityExternalFilesPsiModule myExternalFilesPsiModule;

        public DefineConstraintsInfoAnalyzer(PreProcessingDirectiveCache preProcessingDirectiveCache,
                                             UnityExternalFilesModuleFactory externalFilesPsiModuleFactory)
        {
            myPreProcessingDirectiveCache = preProcessingDirectiveCache;
            myExternalFilesPsiModule = externalFilesPsiModuleFactory.PsiModule.NotNull("externalFilesPsiModuleFactory.PsiModule != null")!;
        }

        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data) =>
            base.ShouldRun(file, data) && IsProjectFileOrKnownExternalFile(data.SourceFile, myExternalFilesPsiModule);

        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if (!element.IsDefineConstraintsArrayEntry())
                return;

            var value = element.GetUnquotedText();
            if (string.IsNullOrWhiteSpace(value)) return;

            if (element.GetContainingFile() is not IJsonNewFile file)
                return;

            var preProcessingDirectives = GetPreProcessingDirectives(file);

            var match = DefineSymbolUtilities.MatchDefineConstraintExpression(value);
            if (match.Success)
            {
                var results = new List<(int index, int length)>();
                var matchGroup = match.Groups["symbol"];
                foreach (Capture capture in matchGroup.Captures)
                {
                    if (capture.Value.StartsWith("!"))
                    {
                        if (HasDefine(preProcessingDirectives, capture.Value[1..]))
                            results.Add((capture.Index, capture.Length));
                    }
                    else
                    {
                        if (!HasDefine(preProcessingDirectives, capture.Value))
                            results.Add((capture.Index, capture.Length));
                    }
                }

                // The defineConstraints entries are treated as AND - all must be met or the assembly definition is not
                // compiled. Constraints in a single entry separated by `||` are OR constraints. If some of them are
                // unmet, highlight them as deadcode. If all are unmet, highlight the entire entry.
                var range = element.GetUnquotedDocumentRange();
                if (results.Count == matchGroup.Captures.Count)
                    consumer.AddHighlighting(new UnmetDefineConstraintInfo(element, range, true));
                else
                {
                    foreach (var (index, length) in results)
                    {
                        var startOffset = range.TextRange.StartOffset + index;
                        var endOffset = startOffset + length;
                        var textRange = new TextRange(startOffset, endOffset);
                        var expressionRange = new DocumentRange(range.Document, textRange);
                        consumer.AddHighlighting(new UnmetDefineConstraintInfo(element, expressionRange, false));
                    }
                }
            }
        }

        private ICollection<PreProcessingDirective> GetPreProcessingDirectives(IJsonNewFile file)
        {
            // Get the defines for the currently edited file. The file might belong to a project, so we could get the
            // actual defines for the project, but that would ignore any changes made to the versionDefines section of
            // the current .asmdef. We also need to use the name from the current .asmdef, as it is the key to the cache
            // and it could have been edited
            var assemblyName = file.GetRootObject()?.GetFirstPropertyValue<IJsonNewLiteralExpression>("name")
                ?.GetUnquotedText();
            if (assemblyName == null || string.IsNullOrWhiteSpace(assemblyName))
                return EmptyList<PreProcessingDirective>.Instance;

            return myPreProcessingDirectiveCache.GetPreProcessingDirectives(assemblyName);
        }

        private static bool HasDefine(IEnumerable<PreProcessingDirective> defines, string symbol) =>
            defines.Any(d => d.Name == symbol);
    }
}