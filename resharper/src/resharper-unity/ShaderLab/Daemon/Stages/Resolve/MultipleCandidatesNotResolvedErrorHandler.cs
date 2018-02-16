using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Resolve
{
    [Language(typeof(ShaderLabLanguage))]
    public class MultipleCandidatesNotResolvedErrorHandler : IResolveProblemHighlighter
    {
        public IHighlighting Run(IReference reference)
        {
            var resolveResult = reference.Resolve();
            if (resolveResult.Result.Candidates.Count > 1)
            {
                var candidates = resolveResult.Result.Candidates;
                var result = new List<string>();
                foreach (var candidate in candidates)
                {
                    result.Add(Environment.NewLine + "  " + DeclaredElementPresenter.Format(ShaderLabLanguage.Instance,
                                   ShaderLabDeclaredElementPresenterStyles.CANDIDATE_PRESENTER, candidate));
                }
                result.Sort();
                var message = result.AggregateString("");
                var range = reference.GetDocumentRange();
                return new MultipleResolveCandidatesError(range, message, reference);
            }

            return null;
        }

        public IEnumerable<ResolveErrorType> ErrorTypes => new[]
        {
            ResolveErrorType.MULTIPLE_CANDIDATES
        };
    }
}