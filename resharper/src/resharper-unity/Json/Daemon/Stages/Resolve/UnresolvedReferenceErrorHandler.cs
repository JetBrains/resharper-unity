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
            // Don't show the error highlight for now - there are too many false positive hits due to references to
            // assembly definitions in .asmdef files that are not part of the solution. These files need to be added
            // into a custom PSI module to make this work properly. This is a quick fix
            // return new UnresolvedProjectReferenceError(reference);
            return null;
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[]
        {
            AsmDefResolveErrorType.ASMDEF_UNRESOLVED_REFERENCED_PROJECT_ERROR
        };
    }
}