using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Utils;
using JetBrains.DocumentModel;
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
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes =
        new[] {typeof(PreferGenericMethodOverloadWarning), typeof(AmbiguousTypeInStringLiteralWarning),
            typeof(UnknownTypeInStringLiteralWarning)})] 
    public class PreferGenericMethodOverloadAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        private delegate bool TypeFilter(ITypeElement typeElement);
        private static readonly IDictionary<string, TypeFilter> ourInterestingMethods = new Dictionary<string, TypeFilter>()
        {
            {"GetComponent", GetComponentTypeFilter},
            {"AddComponent", AddComponentTypeFilter},
            {"CreateInstance", CreateInstanceTypeFilter}
        };
        
        private static readonly ISet<IClrTypeName> ourInterestingClasses = new HashSet<IClrTypeName>()
        {
            KnownTypes.ScriptableObject,
            KnownTypes.GameObject,
            KnownTypes.Component,
        };
        
        public PreferGenericMethodOverloadAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            if (expression.RPar == null) return;

            if (!(expression.InvokedExpression is IReferenceExpression)) return;
            
            var reference = expression.Reference;
            var argument = expression.Arguments.SingleItem?.Value as ILiteralExpression;
            
            if (argument == null || reference == null || expression.TypeArguments.Count != 0) return;
            if (!argument.Literal.IsAnyStringLiteral()) return;

            var methodName = GetMethodName(reference);
            if (methodName == null || !ourInterestingMethods.ContainsKey(methodName)) return;

            var containingClrTypeName = GetContainingClrTypeName(reference);
            if (containingClrTypeName == null || !ourInterestingClasses.Contains(containingClrTypeName)) return;
 
            var stringLiteral = argument.ConstantValue.Value as string;
            if (!ValidityChecker.IsValidDeclaredType(stringLiteral))
            {
                consumer.AddHighlighting(new UnknownTypeInStringLiteralWarning(argument));
                return;
            }

            var types = ResolveStringLiteral(stringLiteral, argument);
            var typesWithRightInheritance = types.Where(t => ourInterestingMethods[methodName](t)).ToArray();
            var suitableTypes = typesWithRightInheritance.Where(t => ImportTypeUtil.TypeIsVisible(t, expression)).ToArray();
            
            if (suitableTypes.Length > 0  && !expression.ContainsPreprocessorDirectives())
            {
                consumer.AddHighlighting(new PreferGenericMethodOverloadWarning(expression, methodName, argument, suitableTypes));
            }

            if (types.Any() && typesWithRightInheritance.Length == 0)
            {
                var inheritedName = containingClrTypeName.Equals(KnownTypes.ScriptableObject)
                    ? KnownTypes.ScriptableObject
                    : KnownTypes.MonoBehaviour;
                consumer.AddHighlighting(new TypeWithWrongInheritanceInStringLiteralWarning(argument, inheritedName.FullName));
                return;
            }
            
            if (types.Length == 0)
            {
                consumer.AddHighlighting(new UnknownTypeInStringLiteralWarning(argument));
                return;
            }
            
            // Notify if ambiguous type
            if (typesWithRightInheritance.Length >= 2)
            {
                consumer.AddHighlighting(new AmbiguousTypeInStringLiteralWarning(argument));
            }
        }

        private static string GetMethodName(IReference reference)
        {
            var info = reference.Resolve();
            
            if (info.ResolveErrorType == ResolveErrorType.OK)
            {
                return info.DeclaredElement?.ShortName;
            }
            return null;
        }
        
        private static IClrTypeName GetContainingClrTypeName([NotNull] IReference reference)
        {
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

        private ITypeElement[] ResolveStringLiteral(string literal, ITreeNode context)
        {
            var factory = CSharpElementFactory.GetInstance(context);
            
            var typeNode = factory.CreateTypeUsageNode(literal, context);
            var typeReference = ((IReferenceName)typeNode.FirstChild)?.Reference;
            if (typeReference == null)
            {
                return Array.Empty<ITypeElement>();
            }
            
            var typeResolver = TypeResolverFactory(typeNode);

            var candidates = typeReference.GetAllNames()
                .SelectMany(typeName => typeResolver(typeName))
                .OfType<ITypeElement>()
                .Where(typeElement => typeElement.GetContainingType() == null 
                                      && typeElement.TypeParameters.Count == 0);
            
            return candidates.ToArray();
        }

        private static bool GetComponentTypeFilter(ITypeElement element)
        {
            var components = element.GetAllSuperClasses().Where(t => t.GetClrName().Equals(KnownTypes.Component)).ToArray();
            var monoScripts = element.GetAllSuperClasses().Where(t => t.GetClrName().Equals(KnownTypes.MonoBehaviour)).ToArray();
            if (components.Any() && monoScripts.Length == 0 && element.GetClrName().FullName.StartsWith("UnityEngine."))
            {
                return true;
            }

            return components.Length == 1 && monoScripts.Any();
        }
        
        private static bool AddComponentTypeFilter(ITypeElement element)
        {
            return GetComponentTypeFilter(element);
        }
        
        private static bool CreateInstanceTypeFilter(ITypeElement element)
        {
            return element.GetAllSuperClasses().Any(t => t.GetClrName().Equals(KnownTypes.ScriptableObject));
        } 
    }
}