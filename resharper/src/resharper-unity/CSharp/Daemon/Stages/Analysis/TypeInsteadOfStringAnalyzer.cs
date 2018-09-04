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
        new[] {typeof(TypeInsteadOfStringUsingWarning), typeof(InvalidStringForTypeWarning)})]
    public class TypeInsteadStringAnalyze : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private static ISet<string> KnownMethods = new HashSet<string>() 
            {
                "GetComponent", 
                "AddComponent",
                "CreateInstance"
            };

        public TypeInsteadStringAnalyze(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (expression.TypeArguments.Count != 0 || expression.Arguments.Count != 1 || !IsComponentKnownMethod(expression)) return;

            var name = expression.Reference.GetName();
            var argument = expression.Arguments.First().Value;
            
            var typeLiteral = ExtractType(argument, out bool isStringLiteral);
            
            if (typeLiteral == null) return;
            if (isStringLiteral)
            {
                if (!CSharpTypeFactory.CreateType(typeLiteral, expression).IsResolved)
                {
                    consumer.AddHighlighting(new InvalidStringForTypeWarning(argument as ILiteralExpression, typeLiteral));
                    return;
                }
            }
            
            consumer.AddHighlighting(new TypeInsteadOfStringUsingWarning(expression, name, argument, typeLiteral));
        }

        private string ExtractType(IExpression argument, out bool isStringLiteral)
        {
            isStringLiteral = false;
            switch (argument)
            {
                case ILiteralExpression literal:
                    if (literal.Literal.IsAnyStringLiteral())
                    {
                        isStringLiteral = true;
                        return literal.ConstantValue.Value as string;
                    }
                    break;
                case ITypeofExpression typeofExpression:
                    return typeofExpression.ArgumentType.GetTypeElement()?.GetClrName().ShortName;
            }
            return null;
        }
        
        private bool IsComponentKnownMethod(IInvocationExpression expression)
        {
            var name = expression?.Reference?.GetName() ?? string.Empty;
            if (!KnownMethods.Contains(name)) return false;
            
            var info = expression.Reference.Resolve();
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