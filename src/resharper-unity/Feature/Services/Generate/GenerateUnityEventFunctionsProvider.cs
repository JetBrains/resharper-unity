using System.Linq;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage))]
    public class GenerateUnityEventFunctionsProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        private readonly UnityApi myUnityApi;

        public GenerateUnityEventFunctionsProvider(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.Project.IsUnityProject())
                return;

            var typeElement = context.ClassDeclaration.DeclaredElement as IClass;
            if (typeElement == null)
                return;

            var unityTypes = myUnityApi.GetBaseUnityTypes(typeElement).ToArray();
            var eventFunctions = unityTypes.SelectMany(h => h.EventFunctions)
                .Where(m => !typeElement.Methods.Any(m.Match)).ToArray();

            var classDeclaration = context.ClassDeclaration;
            var factory = CSharpElementFactory.GetInstance(classDeclaration);
            var methods = eventFunctions
                .Select(e => e.CreateDeclaration(factory, classDeclaration))
                .Select(d => d.DeclaredElement)
                .Where(m => m != null);
            // Make sure we only add a method once (e.g. EditorWindow derives from ScriptableObject
            // and both declare the OnDestroy event function)
            var elements = methods.Select(m => new GeneratorDeclaredElement<IMethod>(m))
                .Distinct(m => m.TestDescriptor);
            context.ProvidedElements.AddRange(elements);
        }

        public override double Priority => 100;
    }
}