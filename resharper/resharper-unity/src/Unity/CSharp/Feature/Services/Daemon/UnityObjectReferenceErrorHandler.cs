using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Daemon
{
    [Language(typeof(CSharpLanguage))]
    public class UnityObjectReferenceErrorHandler : IResolveProblemHighlighter
    {
        // We only get called for our known resolve errors
        public IHighlighting Run(IReference reference)
        {
            var errorType = reference.CheckResolveResult();
            if (errorType == UnityResolveErrorType.UNRESOLVED_COMPONENT_OR_SCRIPTABLE_OBJECT)
                return new UnresolvedComponentOrScriptableObjectWarning(reference);
            if (errorType == UnityResolveErrorType.EXPECTED_COMPONENT)
                return new ExpectedComponentWarning(reference, KnownTypes.Component.FullName);
            if (errorType == UnityResolveErrorType.EXPECTED_MONO_BEHAVIOUR)
                return new ExpectedComponentWarning(reference, KnownTypes.MonoBehaviour.FullName);
            if (errorType == UnityResolveErrorType.EXPECTED_SCRIPTABLE_OBJECT)
                return new ExpectedScriptableObjectWarning(reference);

            return null;
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[]
        {
            UnityResolveErrorType.UNRESOLVED_COMPONENT_OR_SCRIPTABLE_OBJECT,
            UnityResolveErrorType.EXPECTED_COMPONENT,
            UnityResolveErrorType.EXPECTED_MONO_BEHAVIOUR,
            UnityResolveErrorType.EXPECTED_SCRIPTABLE_OBJECT
        };
    }
}