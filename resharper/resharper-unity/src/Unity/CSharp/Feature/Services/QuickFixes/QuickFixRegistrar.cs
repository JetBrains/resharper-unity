using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.CodeCleanup.HighlightingModule;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.QuickFixes.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using static JetBrains.ReSharper.Daemon.CSharp.CodeCleanup.CSharpHighlightingCleanupModule;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    // Most QuickFixes are auto-registered, via [QuickFix] and ctor injection.
    // Manual registration allows us to reuse an existing quick fix with a different highlighting.
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class QuickFixRegistrar : IQuickFixesProvider, IHighlightingCleanupItemsProvider
    {
        public IEnumerable<Type> Dependencies => Array.Empty<Type>();

        public void Register(IQuickFixesRegistrar table)
        {
            table.RegisterQuickFix<RedundantEventFunctionWarning>(
                default,
                h => new RemoveUnusedElementFix(h.MethodDeclaration, Strings.QuickFixRegistrar_Register_Remove_redundant_Unity_event_function),
                typeof(RemoveUnusedElementFix));

            table.RegisterQuickFix<RedundantInitializeOnLoadAttributeWarning>(default, h => new RemoveRedundantAttributeQuickFix(h), typeof(RemoveRedundantAttributeQuickFix));
            table.RegisterQuickFix<RedundantSerializeFieldAttributeWarning>(default, h => new RemoveRedundantAttributeQuickFix(h), typeof(RemoveRedundantAttributeQuickFix));
            table.RegisterQuickFix<RedundantHideInInspectorAttributeWarning>(default, h => new RemoveRedundantAttributeQuickFix(h), typeof(RemoveRedundantAttributeQuickFix));
            table.RegisterQuickFix<RedundantAttributeOnTargetWarning>(default, h => new RemoveRedundantAttributeQuickFix(h), typeof(RemoveRedundantAttributeQuickFix));
            table.RegisterQuickFix<RedundantFormerlySerializedAsAttributeWarning>(default, h => new RemoveRedundantAttributeQuickFix(h), typeof(RemoveRedundantAttributeQuickFix));
        }

        public void Register(IHighlightingCleanupItemsRegistrar registrar)
        {
            registrar.RegisterQuickFix<RedundantInitializeOnLoadAttributeWarning, RemoveRedundantAttributeQuickFix>(REMOVE_REDUNDANCIES, enforceCleanup: false);
            registrar.RegisterQuickFix<RedundantSerializeFieldAttributeWarning, RemoveRedundantAttributeQuickFix>(REMOVE_REDUNDANCIES, enforceCleanup: false);
            registrar.RegisterQuickFix<RedundantHideInInspectorAttributeWarning, RemoveRedundantAttributeQuickFix>(REMOVE_REDUNDANCIES, enforceCleanup: false);
            registrar.RegisterQuickFix<RedundantAttributeOnTargetWarning, RemoveRedundantAttributeQuickFix>(REMOVE_REDUNDANCIES, enforceCleanup: false);
            registrar.RegisterQuickFix<RedundantFormerlySerializedAsAttributeWarning, RemoveRedundantAttributeQuickFix>(REMOVE_REDUNDANCIES, enforceCleanup: false);
        }
    }
}