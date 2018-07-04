using JetBrains.ReSharper.Feature.Services.CSharp.Intentions.DataProviders;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Intentions.CreateDeclaration;
using JetBrains.ReSharper.Feature.Services.Intentions.DataProviders;
using JetBrains.ReSharper.Feature.Services.Intentions.Impl.DeclarationBuilders;
using JetBrains.ReSharper.Feature.Services.Intentions.Impl.TemplateFieldHolders;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class CreateStaticConstructorFromUsageAction :
        CreateFromUsageActionBase2<CreateConstructorDeclarationContext, IReference>
    {
        private readonly IClassLikeDeclaration myClassLikeDeclaration;

        public CreateStaticConstructorFromUsageAction(IAttribute attribute)
            : base(reference: null)
        {
            myClassLikeDeclaration = ClassLikeDeclarationNavigator.GetByAttribute(attribute);
        }

        public override string Text => $"Create static constructor '{myClassLikeDeclaration.DeclaredName}'";

        public override ICreatedElementConsistencyGroup GetConsistencyGroup() => AlwaysConsintentGroup.Instance;

        protected override bool IsAvailableInternal()
        {
            var typeElement = GetTypeElement();
            if (typeElement is IClass || typeElement is IStruct)
                return typeElement.GetDeclarations().Count > 0;
            return false;
        }

        protected override ICreationTarget GetTarget()
        {
            return new TypeTarget(GetTypeElement(), myClassLikeDeclaration);
        }

        private ITypeElement GetTypeElement() => myClassLikeDeclaration.DeclaredElement;

        protected override CreateConstructorDeclarationContext CreateContext()
        {
            var target = GetTarget();

            var psiModule = myClassLikeDeclaration.GetPsiModule();
            var constructorSignature = CSharpMethodSignatureProvider.CreateFromArguments(
                EmptyArray<ICSharpArgument>.Instance, (IType) null, psiModule);

            return new CreateConstructorDeclarationContext
            {
                Class = GetTypeElement(),
                AccessRights = AccessRights.NONE,
                ConstructorSignature = constructorSignature,
                Target = target
            };
        }

        protected override IntentionResult ExecuteIntention(CreateConstructorDeclarationContext context)
        {
            var intention = ConstructorDeclarationBuilder.Create(context);
            var constructor = (IConstructorDeclaration) intention.ResultDeclaration;
            constructor.SetStatic(true);
            return new IntentionResult(EmptyList<ITemplateFieldHolder>.Instance, constructor, constructor.Body.Statements[0].GetDocumentRange());
        }
    }
}