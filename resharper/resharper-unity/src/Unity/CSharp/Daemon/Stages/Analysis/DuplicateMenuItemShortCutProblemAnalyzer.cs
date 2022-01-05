using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Caches;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{

    [ElementProblemAnalyzer(typeof(IAttribute), HighlightingTypes = new[] {typeof(DuplicateShortcutWarning)})]
    public class DuplicateMenuItemShortCutProblemAnalyzer : UnityElementProblemAnalyzer<IAttribute>
    {
        private readonly UnityShortcutCache myCache;

        public DuplicateMenuItemShortCutProblemAnalyzer([NotNull] UnityApi unityApi, UnityShortcutCache cache)
            : base(unityApi)
        {
            myCache = cache;
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var arguments = element.Arguments;
            var argument = UnityShortcutCache.GetArgument(0, "itemName", arguments);
            var name = argument?.Value?.ConstantValue.Value as string;
            if (name == null)
                return;

            var shortcut = UnityShortcutCache.ExtractShortcutFromName(name);
            if (shortcut == null)
                return;

            if (myCache.GetCount(shortcut) > 1)
            {
                var files = myCache.GetSourceFileWithShortCut(shortcut);
                var sourceFile = element.GetSourceFile();
                var anotherFile = files.FirstOrDefault(t => t != sourceFile);
                if (anotherFile == null)
                {
                    consumer.AddHighlighting(new DuplicateShortcutWarning(argument, "this file"));
                }
                else
                {
                    consumer.AddHighlighting(new DuplicateShortcutWarning(argument, anotherFile.Name));
                }
            }
        }
    }

}