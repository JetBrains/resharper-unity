using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class GeneratorUnityKinds
    {
        public const string UnityMessages = "Unity.Messages";
    }

    [GenerateProvider]
    public class MonoBehaviourMethodsWorkflowProvider: IGenerateWorkflowProvider
    {
        public IEnumerable<IGenerateActionWorkflow> CreateWorkflow(IDataContext dataContext)
        {
            return new[] {new GenerateMonoBehaviourMethodsWorkflow()};
        }
    }

    public class GenerateMonoBehaviourMethodsWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateMonoBehaviourMethodsWorkflow() : base(GeneratorUnityKinds.UnityMessages, null, "MonoBehaviour event handlers", GenerateActionGroup.CLR_LANGUAGE, "Event handlers", "", "Generate.MonoBehaviour")
        {
        }

        public override double Order => 100;
    }

    [GeneratorBuilder(GeneratorUnityKinds.UnityMessages, typeof (CSharpLanguage))]
    public class UnityMessageBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        public override double Priority => 100;

        protected override bool HasProcessableElements(CSharpGeneratorContext context, IEnumerable<IGeneratorElement> elements)
        {
            return base.HasProcessableElements(context, elements);
        }

        protected override void BuildOptions(CSharpGeneratorContext context, ICollection<IGeneratorOption> options)
        {
            base.BuildOptions(context, options);
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            var typeElement = context.ClassDeclaration.DeclaredElement as IClass;
            if (typeElement == null)
                return;

            if (!MonoBehaviourUtil.IsMonoBehaviourType(typeElement, context.PsiModule))
                return;
            var selectedMethods = context.InputElements.OfType<GeneratorDeclaredElement<IMethod>>();
            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            foreach (var selectedMethod in selectedMethods)
            {
                //TODO: include parameters when neccessary
                var method = (IMethodDeclaration)factory.CreateTypeMemberDeclaration(
                    $"private void {selectedMethod.DeclaredElement.ShortName}()");
                method.SetBody(factory.CreateEmptyBlock());
                context.PutMemberDeclaration(method);
            }
        }
    }

    [GeneratorElementProvider(GeneratorUnityKinds.UnityMessages, typeof(CSharpLanguage))]
    public class MonoBehaviourMethodsProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        public override void Populate(CSharpGeneratorContext context)
        {
            var typeElement = context.ClassDeclaration.DeclaredElement as IClass;
            if (typeElement == null)
                return;

            if (!MonoBehaviourUtil.IsMonoBehaviourType(typeElement, context.PsiModule))
                return;

            var missingMethods = MonoBehaviourUtil.EventNames.Except(
                typeElement.Methods.Select(m => m.ShortName));

            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);

            foreach (var missingMethod in missingMethods)
            {
                var method = (IMethod)factory.CreateTypeMemberDeclaration($"private void {missingMethod}()").DeclaredElement;
                context.ProvidedElements.Add(new GeneratorDeclaredElement<IMethod>(method));
            }
        }

        public override double Priority => 100;
    }


}