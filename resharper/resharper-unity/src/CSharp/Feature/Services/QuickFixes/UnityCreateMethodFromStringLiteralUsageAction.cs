using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Intentions.CreateDeclaration;
using JetBrains.ReSharper.Feature.Services.Intentions.DataProviders;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
using JetBrains.ReSharper.Intentions.Util;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    public class UnityCreateMethodFromStringLiteralUsageAction : CreateMethodFromUsageAction
    {
        public UnityCreateMethodFromStringLiteralUsageAction(UnityEventFunctionReference reference)
            : base(reference)
        {
        }

        public override ICreatedElementConsistencyGroup GetConsistencyGroup()
        {
            var node = Reference.GetTreeNode().NotNull("node != null");
            var psiServices = node.GetPsiServices();
            var sourceFile = node.GetSourceFile();
            var policyProvider = psiServices.Naming.Policy.GetPolicyProvider(CSharpLanguage.Instance, sourceFile,
                node.GetSettingsStoreWithEditorConfig());
            var namingRule = policyProvider.GetPolicy(NamedElementKinds.Method).NamingRule;
           return  new ConsistencyGroupByNaming(namingRule, true);
        }

        protected override bool IsAvailableInternal()
        {
            return true;
        }

        protected override ICreationTarget GetTarget()
        {
            if (!ValidUtils.Valid(Reference))
                return null;

            var node = Reference.GetTreeNode();
            var @class = node.GetContainingNode<IClassLikeDeclaration>()?.DeclaredElement;
            if (@class == null)
                return null;
            
            return new TypeTarget(@class, node);
        }

        protected override CreateMethodDeclarationContext CreateContext()
        {
            var node = Reference?.GetTreeNode().NotNull("node != null");
            var provider = new MemberSignatureProvider(node.GetPsiServices(), CSharpLanguage.Instance);
            var predefinedType = node.GetPsiModule().GetPredefinedType();
            var signature = provider.CreateFromTypes(predefinedType.IEnumerator, GetParameterTypes(), node.GetSourceFile());
            
            return new CreateMethodDeclarationContext
            {
                MethodName = Reference.GetName(),
                MethodSignatures = new [] { signature },
                TypeArguments = EmptyList<IType>.InstanceList,
                IsAbstract = false,
                IsStatic = false,
                AccessRights = AccessRights.PRIVATE,
                Target = GetTarget()
            };
        }

        private IDeclaredType[] GetParameterTypes()
        {
            var node = Reference.GetTreeNode();
            var argument = CSharpArgumentNavigator.GetByValue(node as ICSharpLiteralExpression);
            var argumentList = ArgumentListNavigator.GetByArgument(argument);
            var invocation = InvocationExpressionNavigator.GetByArgumentList(argumentList);
            if (invocation?.Reference?.Resolve().DeclaredElement?.ShortName
                    .Equals("StartCoroutine") == true && argumentList?.Arguments.Count > 1)
            {
                var secondArgument = argumentList.NotNull("argumentList != null").Arguments[1];
                if (secondArgument.Value?.Type() is IDeclaredType type)
                {
                    return new[] {type};
                }
            }
            
            return new IDeclaredType[] { };
        }
    }
}