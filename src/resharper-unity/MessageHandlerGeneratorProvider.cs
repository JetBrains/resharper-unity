using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
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
                ISubstitution newSubstitution;
                var method = (IMethodDeclaration) CSharpGenerateUtil.CreateMemberDeclaration(
                    context.ClassDeclaration, selectedMethod.Substitution, selectedMethod.DeclaredElement, false, out newSubstitution);
                method.SetBody(factory.CreateEmptyBlock());
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

            if (!MonoBehaviourUtil.IsMonoBehaviourType(typeElement, context.PsiModule))
                return;

            var existingEventNames = typeElement.Methods.Select(m => m.ShortName).ToHashSet();
            var missingEvents = MonoBehaviourUtil.Events.Where(e => !existingEventNames.Contains(e.Name));

            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);

            foreach (var missingEvent in missingEvents)
            {
                var method = CreateDeclaration(missingEvent, factory, context.ClassDeclaration).DeclaredElement;
                if (method != null)
                    context.ProvidedElements.Add(new GeneratorDeclaredElement<IMethod>(method));
            }
        }
        
        [NotNull]
        private IMethodDeclaration CreateDeclaration([NotNull] MonoBehaviourEvent monoBehaviourEvent, [NotNull] CSharpElementFactory elementFactory, [NotNull] IClassLikeDeclaration context)
        {
            var builder = new StringBuilder(128);

            builder.Append("void ");
            builder.Append(monoBehaviourEvent.Name);
            builder.Append("(");

            for (int i = 0; i < monoBehaviourEvent.Parameters.Length; i++)
            {
                if (i > 0)
                    builder.Append(",");
                var parameter = monoBehaviourEvent.Parameters[i];
                builder.Append(parameter.ClrTypeName.FullName);
                if (parameter.IsArray)
                    builder.Append("[]");
                builder.Append(' ');
                builder.Append(parameter.Name);
            }

            builder.Append(");");

            var declaration = (IMethodDeclaration) elementFactory.CreateTypeMemberDeclaration(builder.ToString());
            declaration.SetResolveContextForSandBox(context, SandBoxContextType.Child);
            declaration.FormatNode();
            return declaration;
        }

        public override double Priority => 100;
    }


}