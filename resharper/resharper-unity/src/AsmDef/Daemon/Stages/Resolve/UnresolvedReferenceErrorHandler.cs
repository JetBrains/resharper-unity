using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    [Language(typeof(JsonNewLanguage))]
    public class UnresolvedReferenceErrorHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference) => new UnresolvedProjectReferenceError(reference);

        public IEnumerable<ResolveErrorType> ErrorTypes =>
            new[] { AsmDefResolveErrorType.ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR };
    }
}