using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api.Utils;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage) /* not safe cause of UnityApi */)]
    public class GenerateUnityEventFunctionsProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        private readonly UnityApi myUnityApi;
        private readonly UnityVersion myUnityVersion;
        private readonly KnownTypesCache myKnownTypesCache;

        public GenerateUnityEventFunctionsProvider(UnityApi unityApi, UnityVersion unityVersion,
                                                   KnownTypesCache knownTypesCache)
        {
            myUnityApi = unityApi;
            myUnityVersion = unityVersion;
            myKnownTypesCache = knownTypesCache;
        }

        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.ClassDeclaration.IsFromUnityProject())
                return;

            if (!(context.ClassDeclaration.DeclaredElement is IClass typeElement))
                return;

            // CompactOneToListMap is optimised for the typical use case of only one item per key
            var existingMethods = new CompactOneToListMap<string, IMethod>();
            foreach (var typeMemberInstance in typeElement.GetAllClassMembers<IMethod>())
                existingMethods.AddValue(typeMemberInstance.Member.ShortName, typeMemberInstance.Member);

            var groupingTypeLookup = new Dictionary<IClrTypeName, ITypeElement>();

            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            var elements = new List<GeneratorDeclaredElement>();

            var unityVersion = myUnityVersion.GetActualVersion(context.ClassDeclaration.GetProject());
            var eventFunctions = myUnityApi.GetEventFunctions(typeElement, unityVersion);

            foreach (var eventFunction in eventFunctions.OrderBy(e => e.Name, new UnityEventFunctionComparer()))
            {
                // Note that we handle grouping, but it's off by default, and Rider doesn't save and restore the last
                // used grouping value. We can set EnforceGrouping, but that's a bit too much
                // https://youtrack.jetbrains.com/issue/RIDER-25194
                if (!groupingTypeLookup.TryGetValue(eventFunction.TypeName, out var groupingType))
                {
                    groupingType = myKnownTypesCache.GetByClrTypeName(eventFunction.TypeName, context.PsiModule)
                        .GetTypeElement();
                    groupingTypeLookup.Add(eventFunction.TypeName, groupingType);
                }

                var makeVirtual = false;
                var accessRights = AccessRights.PRIVATE;

                var exactMatch = existingMethods[eventFunction.Name]
                    .FirstOrDefault(m => eventFunction.Match(m) == MethodSignatureMatch.ExactMatch);
                if (exactMatch != null)
                {
                    // Exact match. Only offer to implement if it's virtual and in a base class
                    if (!exactMatch.IsVirtual)
                        continue;

                    var containingType = exactMatch.GetContainingType();
                    if (Equals(containingType, typeElement))
                        continue;

                    makeVirtual = true;
                    accessRights = exactMatch.GetAccessRights();
                    groupingType = containingType;
                }

                var newMethodDeclaration = eventFunction
                    .CreateDeclaration(factory, myKnownTypesCache, context.ClassDeclaration, accessRights, makeVirtual);
                if (makeVirtual)
                {
                    // Make the parameter names are the same as the overridden method, or the "redundant override"
                    // inspection doesn't kick in
                    var overrideParameters = exactMatch.Parameters;
                    var newParameters = newMethodDeclaration.ParameterDeclarations;
                    for (var i = 0; i < overrideParameters.Count; i++)
                        newParameters[i].SetName(overrideParameters[i].ShortName);
                }

                var newMethod = newMethodDeclaration.DeclaredElement;
                Assertion.AssertNotNull(newMethod, "newMethod != null");

                elements.Add(new GeneratorDeclaredElement(newMethod, newMethod.IdSubstitution, groupingType));
            }

            context.ProvidedElements.AddRange(elements.Distinct(m => m.TestDescriptor));
        }

        public override double Priority => 100;
    }
}