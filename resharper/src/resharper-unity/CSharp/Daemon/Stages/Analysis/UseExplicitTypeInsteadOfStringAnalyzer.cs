using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.BuildScripts.DaemonStage.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Goto.Filters;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Intentions.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.DocComments;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(UseExplicitTypeInsteadOfStringUsingWarning), typeof(InvalidStringForTypeWarning)})]
    public class UseExplicitTypeInsteadStringAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private delegate bool TypeFilter(ITypeElement typeElement);
        private static readonly IDictionary<string, TypeFilter> InterestingMethods = new Dictionary<string, TypeFilter>()
        {
            {"GetComponent", GetComponentTypeFilter},
            {"AddComponent", AddComponentTypeFilter},
            {"CreateInstance", CreateInstanceTypeFilter}
        };
        
        private static readonly ISet<IClrTypeName> InterestingClasses = new HashSet<IClrTypeName>()
        {
            KnownTypes.ScriptableObject,
            KnownTypes.GameObject,
            KnownTypes.Component,
        };
        
        public UseExplicitTypeInsteadStringAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (!expression.IsValid()) return;
            if (!ValidityChecker.IsValidExpression(expression.GetText())) return;
            
            var reference = expression.Reference;
            if (expression.TypeArguments.Count != 0 || expression.Arguments.Count != 1 || reference == null) return;

            var methodName = reference.GetName();
            if (!InterestingMethods.ContainsKey(methodName)) return;

            var containingClrTypeName = GetContainingClrTypeName(reference);
            if (!InterestingClasses.Contains(containingClrTypeName)) return;
            
            var argument = expression.Arguments.First().Value;
            var stringLiteral = GetStringLiteralFromArgument(argument);
            if (stringLiteral == null) return;

            var types = ResolveStringLiteral(stringLiteral, methodName, argument);
            var filteredTypes = types.Where(t => InterestingMethods[methodName](t)).ToArray();
            
            if (filteredTypes.Length == 0 && types.Any())
            {
                var inheritedName = containingClrTypeName.Equals(KnownTypes.ScriptableObject)
                    ? KnownTypes.ScriptableObject
                    : KnownTypes.MonoBehaviour;
                consumer.AddHighlighting(new OnlyInvalidTypeInsteadOfStringWarning(argument as ILiteralExpression, stringLiteral, inheritedName.FullName));
                return;
            }
            
            if (filteredTypes.Length == 0)
            {
                consumer.AddHighlighting(new InvalidStringForTypeWarning(argument as ILiteralExpression, stringLiteral));
                return;
            }

            if (filteredTypes.Length >= 2)
            {
                consumer.AddHighlighting(new AmbiguousUnityMonoBehaviourDefinitionWarning(argument as ILiteralExpression, stringLiteral));
            }
            
            consumer.AddHighlighting(new UseExplicitTypeInsteadOfStringUsingWarning(expression, methodName, stringLiteral, argument, filteredTypes));
        }

        private string GetStringLiteralFromArgument(IExpression argument)
        {
            if (argument is ILiteralExpression literal && literal.Literal.IsAnyStringLiteral())
            {
                var result =  literal.ConstantValue.Value as string;
                if (result == null || !ValidityChecker.IsValidIdentifier(result)) return null;
                return result;
            }

            return null;
        }
         
        private IClrTypeName GetContainingClrTypeName([NotNull] IReference reference)
        {
            var name = reference.GetName();

            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK)
            {
                var method = info.DeclaredElement as IMethod;
                return method?.GetContainingType()?.GetClrName();
            }

            return null;
        }

        [NotNull]
        private ImportTypeResolver TypeResolverFactory([NotNull] ITreeNode context)
        {
            var symbolCache = context.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(context.GetPsiModule(), withReferences: true, true);
            return typeName => symbolScope.GetElementsByShortName(typeName);
        }

        private ITypeElement[] ResolveStringLiteral(string literal, string methodName, ITreeNode context)
        {
            var factory = CSharpElementFactory.GetInstance(context);
            
            var typeNode = factory.CreateTypeUsageNode(literal, context);
            var typeReference = (typeNode.FirstChild as IReferenceName).Reference;
            
            var typeResolver = TypeResolverFactory(typeNode);

            var candidates = typeReference.GetAllNames()
                .SelectMany(typeName => typeResolver(typeName))
                .OfType<ITypeElement>()
                .Where(typeElement => ImportTypeUtil.TypeIsVisible(typeElement, typeNode)).ToArray();

            
            return candidates.Where(typeElement => ImportTypeUtil.TypeIsVisible(typeElement, typeNode)).ToArray();
        }

        private static bool GetComponentTypeFilter(ITypeElement element)
        {
            var components = element.GetAllSuperClasses().Where(t => t.GetClrName().Equals(KnownTypes.Component)).ToArray();
            var monoScripts = element.GetAllSuperClasses().Where(t => t.GetClrName().Equals(KnownTypes.MonoBehaviour)).ToArray();
            if (components.Any() && monoScripts.Length == 0 && element.GetClrName().FullName.StartsWith("UnityEngine"))
            {
                return true;
            }

            if (components.Length == 1 && monoScripts.Any())
            {
                return true;
            }
            
            return false;
        }
        
        private static bool AddComponentTypeFilter(ITypeElement element)
        {
            return GetComponentTypeFilter(element);
        }
        
        private static bool CreateInstanceTypeFilter(ITypeElement element)
        {
            var scriptableObjects = element.GetAllSuperClasses().Where(t => t.GetClrName().Equals(KnownTypes.ScriptableObject)).ToArray();
            return scriptableObjects.Any();
        } 
    }
}