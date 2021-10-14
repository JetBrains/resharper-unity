using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
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

        private static readonly Regex ourRegex = new Regex(@"(?<symbol>!?\w+)(\s*\|\|\s*(?<symbol>!?\w+))*", RegexOptions.Compiled);

        // TODO: Move PPDC to asmdef folder structure. We shouldn't depend on CSharp content
        public DefineConstraintsInfoAnalyzer(PreProcessingDirectiveCache preProcessingDirectiveCache,
                                             UnityExternalFilesModuleFactory externalFilesPsiModuleFactory)
        {
            myPreProcessingDirectiveCache = preProcessingDirectiveCache;
            myExternalFilesPsiModule = externalFilesPsiModuleFactory.PsiModule.NotNull("externalFilesPsiModuleFactory.PsiModule != null")!;
        }

        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            // The source file must be either a project file, or a known external Unity file. Don't display anything
            // if the user opens an arbitrary .asmdef file
            var sourceFile = data.SourceFile;
            if (sourceFile == null ||
                (sourceFile.ToProjectFile() == null && !myExternalFilesPsiModule.ContainsFile(sourceFile)))
            {
                return;
            }

            if (!element.IsDefineConstraintsArrayEntry())
                return;

            var value = element.GetUnquotedText();
            if (string.IsNullOrWhiteSpace(value)) return;

            if (element.GetContainingFile() is not IJsonNewFile file)
                return;

            var preProcessingDirectives = GetPreProcessingDirectives(file);

            var match = ourRegex.Match(value);
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
                var fullDocumentRange = element.GetUnquotedDocumentRange();
                if (results.Count == matchGroup.Captures.Count)
                    consumer.AddHighlighting(new UnmetDefineConstraintInfo(element, fullDocumentRange, true));
                else
                {
                    foreach (var (index, length) in results)
                    {
                        var startOffset = fullDocumentRange.TextRange.StartOffset + index;
                        var endOffset = startOffset + length;
                        var textRange = new TextRange(startOffset, endOffset);
                        var documentRange = new DocumentRange(fullDocumentRange.Document, textRange);
                        consumer.AddHighlighting(new UnmetDefineConstraintInfo(element, documentRange, false));
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