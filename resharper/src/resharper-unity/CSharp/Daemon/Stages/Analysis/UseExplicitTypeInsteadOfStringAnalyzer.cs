using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.BuildScripts.DaemonStage.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.DocComments;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(UseExplicitTypeInsteadOfStringUsingWarning), typeof(InvalidStringForTypeWarning)})]
    public class UseExplicitTypeInsteadStringAnalyze : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static readonly ISet<string> KnownMethods = new HashSet<string>()
        {
            "GetComponent", "AddComponent", "CreateInstance"
        };

        public UseExplicitTypeInsteadStringAnalyze(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var reference = expression.Reference;
            if (expression.TypeArguments.Count != 0 || expression.Arguments.Count != 1 || reference == null ||
                !IsComponentKnownMethod(reference)) return;

            var name = reference.GetName();
            var argument = expression.Arguments.First().Value;

            var typeLiteral = ExtractType(argument);

            if (typeLiteral == null) return;
            if (!CSharpTypeFactory.CreateType(typeLiteral, expression).IsResolved)
            {
                consumer.AddHighlighting(new InvalidStringForTypeWarning(argument as ILiteralExpression, typeLiteral));
                return;
            }

            consumer.AddHighlighting(new UseExplicitTypeInsteadOfStringUsingWarning(expression, name, argument, typeLiteral));
        }

        private string ExtractType(IExpression argument)
        {
            if (argument is ILiteralExpression literal && literal.Literal.IsAnyStringLiteral())
            {
                return literal.ConstantValue.Value as string;
            }

            return null;
        }

        private bool IsComponentKnownMethod([NotNull] IReference reference)
        {
            var name = reference.GetName();
            if (!KnownMethods.Contains(name)) return false;

            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK)
            {
                var method = info.DeclaredElement as IMethod;
                var containingType = method?.GetContainingType();
                if (containingType != null)
                {
                    var qualifierTypeName = containingType.GetClrName();
                    return KnownTypes.Component.Equals(qualifierTypeName) ||
                           KnownTypes.GameObject.Equals(qualifierTypeName) ||
                           KnownTypes.ScriptableObject.Equals(qualifierTypeName);
                }
            }

            return false;
        }
    }
}