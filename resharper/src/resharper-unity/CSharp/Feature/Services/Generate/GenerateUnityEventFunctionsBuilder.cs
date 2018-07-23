using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityEventFunctions, typeof (CSharpLanguage))]
    public class GenerateUnityEventFunctionsBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        private readonly UnityApi myUnityApi;

        public GenerateUnityEventFunctionsBuilder(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        public override double Priority => 100;

        // Enables/disables the menu item
        protected override bool IsAvaliable(CSharpGeneratorContext context)
        {
            return context.Project.IsUnityProject() && HasUnityBaseType(context) && base.IsAvaliable(context);
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            if (!HasUnityBaseType(context)) return;

            var selectedMethods = context.InputElements.OfType<GeneratorDeclaredElement<IMethod>>();
            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            foreach (var selectedMethod in selectedMethods)
            {
                ISubstitution newSubstitution;
                var method = (IMethodDeclaration) CSharpGenerateUtil.CreateMemberDeclaration(
                    context.ClassDeclaration, selectedMethod.Substitution, selectedMethod.DeclaredElement, false, out newSubstitution);
                method.SetStatic(selectedMethod.DeclaredElement.IsStatic);
                method.SetBody(factory.CreateBlock("{throw new System.NotImplementedException();}"));
                method.FormatNode();
                context.PutMemberDeclaration(method);
            }
        }

        private bool HasUnityBaseType(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement is IClass typeElement && myUnityApi.IsUnityType(typeElement);
        }
    }
}