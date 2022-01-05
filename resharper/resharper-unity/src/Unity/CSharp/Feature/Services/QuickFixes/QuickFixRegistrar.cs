using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.QuickFixes.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    // Most QuickFixes are auto-registered, via [QuickFix] and ctor injection.
    // Manual registration allows us to reuse an existing quick fix with a different highlighting.
    [ShellComponent]
    public class QuickFixRegistrar : IQuickFixesProvider
    {
        private readonly Lifetime myLifetime;

        public QuickFixRegistrar(Lifetime lifetime)
        {
            myLifetime = lifetime;
        }

        public void Register(IQuickFixesRegistrar table)
        {
            table.RegisterQuickFix<RedundantEventFunctionWarning>(myLifetime,
                h => new RemoveUnusedElementFix(h.MethodDeclaration, "Remove redundant Unity event function"),
                typeof(RemoveUnusedElementFix));
        }

        public IEnumerable<Type> Dependencies => Array.Empty<Type>();
    }
}