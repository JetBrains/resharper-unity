using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    public abstract class MethodSignatureProblemAnalyzerBase<T> : UnityElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected MethodSignatureProblemAnalyzerBase([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected void AddMethodSignatureInspections(IHighlightingConsumer consumer,
            IMethodDeclaration methodDeclaration, MethodSignature expectedMethodSignature, MethodSignatureMatch match)
        {
            if (match == MethodSignatureMatch.NoMatch || match == MethodSignatureMatch.ExactMatch)
                return;

            // If the value is not a power of two, that means more than one flag has been set
            if (((int) match & ((int) match - 1)) != 0)
            {
                consumer.AddHighlighting(new IncorrectSignatureWarning(methodDeclaration, expectedMethodSignature, match));
            }
            else
            {
                if ((match & MethodSignatureMatch.IncorrectStaticModifier) == MethodSignatureMatch.IncorrectStaticModifier)
                    consumer.AddHighlighting(new InvalidStaticModifierWarning(methodDeclaration, expectedMethodSignature));
                if ((match & MethodSignatureMatch.IncorrectReturnType) == MethodSignatureMatch.IncorrectReturnType)
                    consumer.AddHighlighting(new InvalidReturnTypeWarning(methodDeclaration, expectedMethodSignature));
                if ((match & MethodSignatureMatch.IncorrectParameters) == MethodSignatureMatch.IncorrectParameters)
                    consumer.AddHighlighting(new InvalidParametersWarning(methodDeclaration, expectedMethodSignature));
                if ((match & MethodSignatureMatch.IncorrectTypeParameters) == MethodSignatureMatch.IncorrectTypeParameters)
                    consumer.AddHighlighting(new InvalidTypeParametersWarning(methodDeclaration, expectedMethodSignature));
            }
        }
    }
}