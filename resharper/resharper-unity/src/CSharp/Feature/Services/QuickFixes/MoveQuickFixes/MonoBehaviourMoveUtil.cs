using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    public static class MonoBehaviourMoveUtil
    {

        [CanBeNull]
        public static IMethodDeclaration GetMonoBehaviourMethod([NotNull] IClassDeclaration classDeclaration, [NotNull] string name)
        {
            return classDeclaration.MethodDeclarations.FirstOrDefault(t => t.NameIdentifier.Name.Equals(name));
        }
        
        [NotNull]
        public static IMethodDeclaration GetOrCreateMethod([NotNull] IClassDeclaration classDeclaration, [NotNull] string methodName)
        {
            var result = GetMonoBehaviourMethod(classDeclaration, methodName);
            if (result == null)
            {
                var factory = CSharpElementFactory.GetInstance(classDeclaration);
                var declaration = (IMethodDeclaration)factory.CreateTypeMemberDeclaration("void $0(){}", methodName);

                result = classDeclaration.AddClassMemberDeclarationAfter(declaration, classDeclaration.FieldDeclarations.FirstOrDefault());
               
            }
            return result;
        }

        public static bool IsExpressionAccessibleInScript([NotNull]ICSharpExpression expression)
        {
            if (!expression.IsValid())
                return false;
            
            var methodDeclaration = expression.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null)
                return false;
            
            var statement = expression.GetContainingStatementLike();
            if (statement == null)
                return false;

// Should we allow to extract under conditions? ifstatement and cycles            
//            if (statement.Parent != expression.GetContainingNode<IMethodDeclaration>()?.Body)
//                return false;

            var classDeclaration = expression.GetContainingNode<IClassDeclaration>();
            if (classDeclaration == null)
                return false;

            if (expression is IThisExpression) 
                return true;
            
            
            IReferenceExpression forAccessCheck;
            switch (expression)
            {
                case IReferenceExpression qualifierReferenceExpression:
                    forAccessCheck = qualifierReferenceExpression;
                    break;
                case IInvocationExpression invocation:
                    forAccessCheck = invocation.InvokedExpression as IReferenceExpression;
                    break;
                default:
                    return false;
            }
                
                            
            var declaredElement = forAccessCheck?.Reference.Resolve().DeclaredElement;

            var typeMember = declaredElement as ITypeMember;
            if (typeMember == null)
                return false;
            
            if (!AccessUtil.IsSymbolAccessible(typeMember, new ElementAccessContext(classDeclaration)))
                return false;

            // costly check
            return IsAvailableToMoveFromScope(expression, methodDeclaration);
        }

        public static bool IsAvailableToMoveFromScope([NotNull] ITreeNode toMove, [NotNull] ITreeNode scope, ITreeNode excludedScope = null)
        {
            var range = toMove.GetDocumentRange();
            var allLocalDeclaration = GetLocalDeclaration(scope, excludedScope ?? toMove);
            var usageProvider = DefaultUsagesProvider.Instance;
            foreach (var localDeclaration in allLocalDeclaration)
            {
                var localDeclaredElement = localDeclaration.DeclaredElement;
                if (localDeclaredElement == null)
                    continue;

                if (usageProvider.GetUsages(localDeclaredElement, toMove).Any())
                    return false;
            }
            return true;
        }

        private static IEnumerable<IDeclaration> GetLocalDeclaration(ITreeNode scope, ITreeNode stopBarrier)
        {
            var enumerator = scope.Descendants();

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current == stopBarrier)
                    yield break;
                switch (current)
                {
                    case ICSharpClosure _:
                        enumerator.SkipThisNode();
                        break;
                    case IDeclaration declaration:
                        yield return declaration;
                        break;
                }
            }
        }
        
        public static void MoveToMethodWithFieldIntroduction([NotNull]IClassDeclaration classDeclaration, [NotNull]ICSharpExpression expression, [NotNull] string methodName, string fieldName = null)
        {
            var methodDeclaration = GetOrCreateMethod(classDeclaration, methodName);
            MoveToMethodWithFieldIntroduction(classDeclaration, methodDeclaration, expression, fieldName);
        }
        
        public static void MoveToMethodWithFieldIntroduction([NotNull]IClassDeclaration classDeclaration,[NotNull] IMethodDeclaration methodDeclaration,
            [NotNull]ICSharpExpression expression, string fieldName = null)
        {
            var result = GetDeclaredElementFromParentDeclaration(expression);
            
            var factory = CSharpElementFactory.GetInstance(classDeclaration);

            var type = expression.Type(new ResolveContext(classDeclaration.GetPsiModule()));

            var baseName = fieldName ?? CreateBaseName(expression, result);
            var name = NamingUtil.GetUniqueName(expression, baseName, NamedElementKinds.PrivateInstanceFields, de => !de.Equals(result));

            var field = factory.CreateFieldDeclaration(type, name);
            field.SetAccessRights(AccessRights.PRIVATE);
            
            classDeclaration.AddClassMemberDeclaration(field);

            var initialization = factory.CreateStatement("$0 = $1;", name, expression.Copy());
            var body = methodDeclaration.EnsureStatementMemberBody();
            body.AddStatementAfter(initialization, null);

            if (expression.Parent is IExpressionStatement statement)
            {
                statement.RemoveOrReplaceByEmptyStatement();
            }
            else
            {
                if (result == null)
                {
                    expression.ReplaceBy(factory.CreateReferenceExpression(name));
                }
                else if (!name.Equals(result.ShortName))
                {
                    var provider = DefaultUsagesProvider.Instance;
                    var usages = provider.GetUsages(result, expression.GetContainingNode<IMethodDeclaration>().NotNull("scope != null"));
                    expression.GetContainingStatement().NotNull("expression.GetContainingStatement() != null").RemoveOrReplaceByEmptyStatement();
                    foreach (var usage in usages)
                    {
                        if (usage.IsValid() && usage is IReferenceExpression node)
                            node.ReplaceBy(factory.CreateReferenceExpression(name));
                    }
                }
            }
        }

        public static IDeclaredElement GetDeclaredElementFromParentDeclaration(ICSharpExpression expression)
        {
            var localVariableDeclaration =
                LocalVariableDeclarationNavigator.GetByInitial(
                    expression.GetContainingParenthesizedExpression()?.Parent as IVariableInitializer);
            return localVariableDeclaration?.DeclaredElement;
        }

        public static string CreateBaseName([NotNull]ICSharpExpression toMove, [CanBeNull] IDeclaredElement declaredElement)
        {
            var type = toMove.Type(new ResolveContext(toMove.GetPsiModule()));
            string baseName =  type.GetPresentableName(CSharpLanguage.Instance);

            if (declaredElement != null)
            {
                baseName = declaredElement.ShortName;
            }
            else
            {
                if (toMove is IInvocationExpression invocationExpression)
                {
                    var arguments = invocationExpression.Arguments;
                    if (arguments.Count > 0)
                    {
                        var argument = arguments[0].Value;
                        var reference = argument.GetReferences<UnityObjectTypeOrNamespaceReference>().FirstOrDefault();
                        if (reference != null && reference.Resolve().ResolveErrorType.IsAcceptable)    
                        {
                            baseName = (argument.ConstantValue.Value as string).NotNull(
                                "argument.ConstantValue.Value as string != null");
                        }
                    }
                }
            }

            return baseName;
        }
    }
}