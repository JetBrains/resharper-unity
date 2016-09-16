using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

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
        public GenerateMonoBehaviourMethodsWorkflow() : base(GeneratorUnityKinds.UnityMessages, null, "Unity3D Messages", GenerateActionGroup.CLR_LANGUAGE, "Unity3D Messages", "", "Generate.MonoBehaviour")
        {
        }

        public override double Order => 100;
    }

    [GeneratorBuilder(GeneratorUnityKinds.UnityMessages, typeof (CSharpLanguage))]
    public class UnityMessageBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        public override double Priority => 100;

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            var typeElement = context.ClassDeclaration.DeclaredElement as IClass;
            if (typeElement == null)
                return;

            if (!typeElement.IsMessageHost())
                return;
            var selectedMethods = context.InputElements.OfType<GeneratorDeclaredElement<IMethod>>();
            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            foreach (var selectedMethod in selectedMethods)
            {
                ISubstitution newSubstitution;
                var method = (IMethodDeclaration) CSharpGenerateUtil.CreateMemberDeclaration(
                    context.ClassDeclaration, selectedMethod.Substitution, selectedMethod.DeclaredElement, false, out newSubstitution);
                method.SetBody(method.Type.IsVoid() ? factory.CreateEmptyBlock() : factory.CreateBlock("{return null;}"));
                method.FormatNode();
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

            var hosts = typeElement.GetMessageHosts().ToArray();
            var events = hosts.SelectMany(h => h.Messages)
                .Where(m => !typeElement.Methods.Any(m.Match)).ToArray();

            var classDeclaration = context.ClassDeclaration;
            var factory = CSharpElementFactory.GetInstance(classDeclaration);
            var methods = events
                .Select(e => e.CreateDeclaration(factory, classDeclaration))
                .Select(d => d.DeclaredElement)
                .Where(m => m != null);
            IEnumerable<IGeneratorElement> elements =
                methods.Select(m => new GeneratorDeclaredElement<IMethod>(m));
            context.ProvidedElements.AddRange(elements);
        }

        public override double Priority => 100;
    }
}