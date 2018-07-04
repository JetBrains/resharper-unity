using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.QuickFixes.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    // Most QuickFixes are auto-registered, via [QuickFix] and ctor injection.
    // Manual registration allows us to reuse an existing quick fix with a different highlighting.
    [ShellComponent]
    public class QuickFixRegistrar
    {
        public QuickFixRegistrar(IQuickFixes table)
        {
            table.RegisterQuickFix<RedundantEventFunctionWarning>(null,
                h => new RemoveUnusedElementFix(h.MethodDeclaration, "Remove redundant Unity event function"),
                typeof(RemoveUnusedElementFix));
        }
    }
}