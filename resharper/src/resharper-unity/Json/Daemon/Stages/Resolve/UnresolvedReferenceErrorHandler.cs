using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Stages.Resolve
{
    [Language(typeof(JsonLanguage))]
    public class UnresolvedReferenceErrorHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference)
        {
            return new UnresolvedProjectReferenceError(reference);
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[]
        {
            AsmDefResolveErrorType.ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR
        };
    }
}