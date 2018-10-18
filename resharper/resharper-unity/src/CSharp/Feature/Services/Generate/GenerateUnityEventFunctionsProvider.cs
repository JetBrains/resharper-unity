using System.Linq;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;

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

            var unityVersion = myUnityVersion.GetActualVersion(context.Project);
            var eventFunctions = myUnityApi.GetEventFunctions(typeElement, unityVersion)
                .Where(f => typeElement.Methods.All(m => f.Match(m) == MethodSignatureMatch.NoMatch)).ToArray();

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