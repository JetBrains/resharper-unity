using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [Language(typeof(JsonNewLanguage))]
    public class UnresolvedReferenceHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference) => new UnresolvedProjectReferenceWarning(reference);

        public IEnumerable<ResolveErrorType> ErrorTypes =>
            new[] { AsmDefResolveErrorType.UNRESOLVED_REFERENCED_ASMDEF_ERROR };
    }
}