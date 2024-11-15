#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.dataStructures;
using MethodSignature = JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api.MethodSignature;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(Instantiation.DemandAnyThreadSafe, typeof(IAttribute))]
    public class AttributedMethodSignatureProblemAnalyzer : MethodSignatureProblemAnalyzerBase<IAttribute>
    {
        // These attributes either don't have RequiredSignatureAttribute, or only added it later, in which case, we
        // still need to provide a known signature. Note that we do understand and provide method signatures for more
        // attributes, as long as they have one or more methods marked with RequiredSignature.
        // Also note that all of these attributes are added to the external annotations, but the usage suppressor will
        // also mark methods as in use if they have an attribute that is marked with RequiredSignature
        private static readonly JetHashSet<IClrTypeName> ourKnownAttributes = new JetHashSet<IClrTypeName>
        {
            // No RequiredSignature (as of Unity 2020.2)
            KnownTypes.InitializeOnLoadMethodAttribute,
            KnownTypes.RuntimeInitializeOnLoadMethodAttribute,

            // These attributes had RequiredSignature added in 2018.3
            KnownTypes.DidReloadScripts,
            KnownTypes.OnOpenAssetAttribute,
            KnownTypes.PostProcessBuildAttribute,
            KnownTypes.PostProcessSceneAttribute,
            KnownTypes.PreferenceItem
        };

        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly KnownTypesCache myKnownTypesCache;
        private readonly ConcurrentDictionary<IClrTypeName, MethodSignature[]> myMethodSignatures;

        public AttributedMethodSignatureProblemAnalyzer(UnityApi unityApi, IPredefinedTypeCache predefinedTypeCache,
                                                        KnownTypesCache knownTypesCache)
            : base(unityApi)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            myKnownTypesCache = knownTypesCache;
            myMethodSignatures = new ConcurrentDictionary<IClrTypeName, MethodSignature[]>();
        }

        protected override void Analyze(IAttribute element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (!(element.TypeReference?.Resolve().DeclaredElement is ITypeElement attributeTypeElement))
                return;

            if (element.Target == AttributeTarget.Return)
                return;

            var methodDeclaration = MethodDeclarationNavigator.GetByAttribute(element);
            if (methodDeclaration == null) return;

            var predefinedType = myPredefinedTypeCache.GetOrCreatePredefinedType(element.GetPsiModule());
            var expectedMethodSignatures = GetExpectedMethodSignatures(attributeTypeElement, element, predefinedType);
            if (expectedMethodSignatures == null) return;

            if (expectedMethodSignatures.Length == 1)
            {
                var match = expectedMethodSignatures[0].Match(methodDeclaration);
                AddMethodSignatureInspections(consumer, methodDeclaration, expectedMethodSignatures[0], match);
            }
            else
            {
                foreach (var methodSignature in expectedMethodSignatures)
                {
                    if (methodSignature.Match(methodDeclaration) == MethodSignatureMatch.ExactMatch)
                        return;
                }

                AddMethodSignatureInspections(consumer, methodDeclaration, expectedMethodSignatures);
            }
        }

        private MethodSignature[]? GetExpectedMethodSignatures(ITypeElement attributeTypeElement,
                                                               IAttribute attribute,
                                                               PredefinedType predefinedType)
        {
            var attributeClrName = attributeTypeElement.GetClrName();
            if (myMethodSignatures.TryGetValue(attributeClrName, out var signatures))
            {
                if (signatures.All(s => s.IsValid()))
                    return signatures;

                // If any of the signatures are no longer valid, clear the cache and try again
                myMethodSignatures.Clear();
            }

            // Try to get the expected signature from a method marked with RequiredSignatureAttribute (introduced in
            // Unity 2017.3). If the attribute does not exist, either because it's not applied, or we're on an earlier
            // version, check our known attributes for a signature
            // Remember that we're run on pooled threads, so try to add to the concurrent dictionary. It doesn't matter
            // if another thread beats us.
            signatures = GetSignaturesFromRequiredSignatureAttribute(attributeTypeElement)
                         ?? GetSignaturesFromKnownAttributes(attributeClrName, predefinedType);
            if (signatures != null) myMethodSignatures.TryAdd(attributeClrName.GetPersistent(), signatures);
            else signatures = GetNonCacheableSignatures(attribute, attributeClrName, predefinedType);
            return signatures;
        }

        private MethodSignature[]? GetSignaturesFromRequiredSignatureAttribute(ITypeElement attributeTypeElement)
        {
            var signatures = new FrugalLocalList<MethodSignature>();
            foreach (var method in attributeTypeElement.Methods)
            {
                if (method.HasAttributeInstance(KnownTypes.RequiredSignatureAttribute, AttributesSource.Self))
                {
                    var parameterTypes = new FrugalLocalList<IType>();
                    var parameterNames = new FrugalLocalList<string>();
                    foreach (var parameter in method.Parameters)
                    {
                        parameterTypes.Add(parameter.Type);
                        parameterNames.Add(parameter.ShortName);
                    }

                    signatures.Add(new MethodSignature(method.ReturnType, method.IsStatic,
                        parameterTypes.AsIReadOnlyList(), parameterNames.AsIReadOnlyList()));
                }
            }

            return signatures.IsEmpty ? null : signatures.ToArray();
        }

        private MethodSignature[]? GetSignaturesFromKnownAttributes(IClrTypeName attributeClrName,
                                                                    PredefinedType predefinedType)
        {
            if (ourKnownAttributes.Contains(attributeClrName))
            {
                // All of our attributes require a static void method with no arguments, apart from a couple that are
                // special cases
                return GetSpecialCaseSignatures(attributeClrName, predefinedType)
                       ?? new[] {GetStaticVoidMethodSignature(predefinedType)};
            }

            return null;
        }

        private MethodSignature[]? GetSpecialCaseSignatures(IClrTypeName attributeClrName,
                                                            PredefinedType predefinedType)
        {
            if (Equals(attributeClrName, KnownTypes.OnOpenAssetAttribute))
                return GetOnOpenAssetMethodSignature(predefinedType);
            if (Equals(attributeClrName, KnownTypes.PostProcessBuildAttribute))
                return GetPostProcessBuildMethodSignature(predefinedType);
            return null;
        }

        private MethodSignature[]? GetNonCacheableSignatures(IAttribute attribute,
                                                             IClrTypeName attributeClrName,
                                                             PredefinedType predefinedType)
        {
            if (Equals(attributeClrName, KnownTypes.MenuItemAttribute))
                return GetMenuItemMethodSignature(attribute, predefinedType);
            return null;
        }

        private static MethodSignature GetStaticVoidMethodSignature(PredefinedType predefinedType) =>
            new(predefinedType.Void, true);

        private MethodSignature[] GetMenuItemMethodSignature(IAttribute attribute, PredefinedType predefinedType)
        {
            IExpression? validateArgExpression = null;
            if (attribute.Arguments.Count > 1)
            {
                // [MenuItem("Something", true|false)]
                validateArgExpression = attribute.Arguments[1].Expression;
            }
            else
            {
                // [MenuItem("Something", validate = true|false)]
                foreach (var assignment in attribute.PropertyAssignments)
                {
                    if (assignment.PropertyNameIdentifier?.Name == "validate")
                    {
                        validateArgExpression = assignment.Source;
                        break;
                    }
                }
            }

            var returnType = predefinedType.Void;
            if (validateArgExpression != null && validateArgExpression.IsConstantValue() &&
                validateArgExpression.ConstantValue.IsBoolean(out var validate) && validate)
            {
                returnType = predefinedType.Bool;
            }

            var menuCommandType = myKnownTypesCache.GetByClrTypeName(KnownTypes.MenuCommand, predefinedType.Module);
            return new[]
            {
                new MethodSignature(returnType, true),
                new MethodSignature(returnType, true, new[] { menuCommandType }, new[] { "menuCommand" })
            };
        }

        private static MethodSignature[] GetOnOpenAssetMethodSignature(PredefinedType predefinedType)
        {
            // Note that since 2019.2, there is an additional signature of
            // private static bool OnOpen(int instanceID, int line, int column)
            // This will be found by the RequiredSignature check
            return new[]
            {
                new MethodSignature(predefinedType.Bool, true,
                    new[] {predefinedType.Int, predefinedType.Int},
                    new[] {"instanceID", "line"})
            };
        }

        // This has RequiredSignature
        private MethodSignature[] GetPostProcessBuildMethodSignature(PredefinedType predefinedType)
        {
            var buildTargetType = myKnownTypesCache.GetByClrTypeName(KnownTypes.BuildTarget, predefinedType.Module);
            return new[]
            {
                new MethodSignature(predefinedType.Void, true,
                    new[] {buildTargetType, predefinedType.String},
                    new[] {"target", "pathToBuildProject"})
            };
        }
    }
}
