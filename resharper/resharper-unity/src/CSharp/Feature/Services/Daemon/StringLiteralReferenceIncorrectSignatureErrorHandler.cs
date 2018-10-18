using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Daemon
{
    [Language(typeof(CSharpLanguage))]
    public class StringLiteralReferenceIncorrectSignatureErrorHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference)
        {
            if (reference is SyncVarHookReference syncVarHookReference)
                return new StringLiteralReferenceIncorrectSignatureError(syncVarHookReference);
            if (reference is UnityEventFunctionReference eventFunctionReference)
                return new StringLiteralReferenceIncorrectSignatureWarning(eventFunctionReference);
            return null;
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[]
        {
            UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_ERROR,
            UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_WARNING
        };
    }
}