using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Daemon
{
    [Language(typeof(CSharpLanguage))]
    public class StringLiteralReferenceIncorrectSignatureErrorHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference)
        {
            return new StringLiteralReferenceIncorrectSignatureError(reference);
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[] {UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE};
    }
}