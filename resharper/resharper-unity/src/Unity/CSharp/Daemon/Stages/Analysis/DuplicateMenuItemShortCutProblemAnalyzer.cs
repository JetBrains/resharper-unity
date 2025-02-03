#nullable enable

using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Caches;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{

    [ElementProblemAnalyzer(typeof(IAttribute), HighlightingTypes = new[] {typeof(DuplicateShortcutWarning)})]
    public class DuplicateMenuItemShortCutProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        private readonly UnityShortcutCache myCache;

        public DuplicateMenuItemShortCutProblemAnalyzer(UnityApi unityApi, UnityShortcutCache cache)
            : base(unityApi)
        {
            myCache = cache;
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var arguments = element.Arguments;
            var argument = UnityShortcutCache.GetArgument(0, "itemName", arguments);

            // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
            // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain that the
            // out variable is uninitialised when we use conditional access
            // See also https://youtrack.jetbrains.com/issue/RSRP-489147
            if (argument?.Value != null && argument.Value.ConstantValue.IsNotNullString(out var name))
            {
                var shortcut = UnityShortcutCache.ExtractShortcutFromName(name);
                if (shortcut == null)
                    return;

                if (myCache.GetCount(shortcut) > 1)
                {
                    var files = myCache.GetSourceFileWithShortCut(shortcut);
                    var sourceFile = element.GetSourceFile();
                    var anotherFile = files.FirstOrDefault(t => t != sourceFile);
                    consumer.AddHighlighting(anotherFile == null
                        ? new DuplicateShortcutWarning(argument,
                            Strings.DuplicateMenuItemShortCutProblemAnalyzer_Analyze_this_file)
                        : new DuplicateShortcutWarning(argument, anotherFile.Name));
                }
            }
        }
    }
}
