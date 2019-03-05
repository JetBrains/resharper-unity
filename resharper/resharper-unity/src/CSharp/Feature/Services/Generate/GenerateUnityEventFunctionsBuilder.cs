using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

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

            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            foreach (var generatorElement in selectedGeneratorElements)
            {
                if (!(generatorElement.DeclaredElement is IMethod selectedMethod)) continue;

                var methodDeclaration = (IMethodDeclaration) CSharpGenerateUtil.CreateMemberDeclaration(
                    context.ClassDeclaration, generatorElement.Substitution, selectedMethod, false, out _);

                methodDeclaration.SetAccessRights(selectedMethod.GetAccessRights());
                methodDeclaration.SetStatic(selectedMethod.IsStatic);

                IBlock block;
                if (selectedMethod.IsVirtual)
                {
                    methodDeclaration.SetOverride(true);
                    var parameters = string.Join(",", selectedMethod.Parameters.Select(p => p.ShortName));
                    block = factory.CreateBlock("{base.$0($1);}", selectedMethod.ShortName, parameters);
                }
                else
                {
                    // The C# generator context will recognise an inserted throw statement and select it, ready for
                    // editing. This doesn't happen when calling base virtual methods
                    var predefinedType = methodDeclaration.GetPredefinedType();
                    block = factory.CreateBlock("{throw new $0();}", predefinedType.NotImplementedException);
                }

                methodDeclaration.SetBody(block);
                methodDeclaration.FormatNode();
                context.PutMemberDeclaration(methodDeclaration);
            }
        }

        private bool HasUnityBaseType(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement is IClass typeElement && myUnityApi.IsUnityType(typeElement);
        }
    }
}