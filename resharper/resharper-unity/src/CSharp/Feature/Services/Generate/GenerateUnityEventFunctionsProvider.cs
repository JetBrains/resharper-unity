using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage))]
    public class GenerateUnityEventFunctionsProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        private readonly UnityApi myUnityApi;
        private readonly UnityVersion myUnityVersion;

        public GenerateUnityEventFunctionsProvider(UnityApi unityApi, UnityVersion unityVersion)
        {
            myUnityApi = unityApi;
            myUnityVersion = unityVersion;
        }

        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.Project.IsUnityProject())
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

            var unityVersion = myUnityVersion.GetActualVersion(context.Project);
            var eventFunctions = myUnityApi.GetEventFunctions(typeElement, unityVersion);

            foreach (var eventFunction in eventFunctions.OrderBy(e => e.Name, new UnityEventFunctionComparer()))
            {
                // Note that we handle grouping, but it's off by default, and Rider doesn't save and restore the last
                // used grouping value. We can set EnforceGrouping, but that's a bit too much
                // https://youtrack.jetbrains.com/issue/RIDER-25194
                if (!groupingTypeLookup.TryGetValue(eventFunction.TypeName, out var groupingType))
                {
                    groupingType = TypeFactory.CreateTypeByCLRName(eventFunction.TypeName, context.PsiModule)
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
                    .CreateDeclaration(factory, context.ClassDeclaration, accessRights, makeVirtual);
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

        // Sort event functions, mostly alphabetically, but with some commonly used messages at the top
        private class UnityEventFunctionComparer : IComparer<string>
        {
            private static readonly OrderedHashSet<string> ourSpecialNames = new OrderedHashSet<string>();

            static UnityEventFunctionComparer()
            {
                ourSpecialNames.Add("Awake");
                ourSpecialNames.Add("Reset");
                ourSpecialNames.Add("Start");
                ourSpecialNames.Add("Update");
                ourSpecialNames.Add("FixedUpdate");
                ourSpecialNames.Add("LateUpdate");
                ourSpecialNames.Add("OnEnable");
                ourSpecialNames.Add("OnDisable");
                ourSpecialNames.Add("OnDestroy");
                ourSpecialNames.Add("OnGUI");
            }

            public int Compare(string x, string y)
            {
                var xi = ourSpecialNames.IndexOf(x);
                var yi = ourSpecialNames.IndexOf(y);
                // -1 -> x is less than y, so goes to top
                if (xi == -1 && yi > -1)
                    return 1;
                if (xi > -1 && yi == -1)
                    return -1;
                if (xi == -1 && yi == -1)
                    return string.Compare(x, y, StringComparison.InvariantCulture);
                return xi > yi ? 1 : (xi < yi ? -1 : 0);
            }
        }
    }
}