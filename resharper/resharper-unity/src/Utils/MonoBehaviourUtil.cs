using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
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

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class MonoBehaviourUtil
    {
        [CanBeNull]
        public static IMethodDeclaration GetStart([NotNull]IClassDeclaration classDeclaration)
        {
            return classDeclaration.MethodDeclarations.FirstOrDefault(t => t.NameIdentifier.Name.Equals("Start"));
        }

        [NotNull]
        public static IMethodDeclaration GetOrCreateStart([NotNull] IClassDeclaration classDeclaration)
        {
            var result = GetStart(classDeclaration);
            if (result == null)
            {
                var factory = CSharpElementFactory.GetInstance(classDeclaration);
                var declaration = (IMethodDeclaration)factory.CreateTypeMemberDeclaration("void Start(){}");

                result = classDeclaration.AddClassMemberDeclarationAfter(declaration, classDeclaration.FieldDeclarations.FirstOrDefault());
               
            }
            return result;
        }
        
        public static void MoveToStart([NotNull]IClassDeclaration classDeclaration, ICSharpStatement statement)
        {
            var start = GetOrCreateStart(classDeclaration);
            var body = start.EnsureStatementMemberBody();
            body.AddStatementAfter(statement, body.Statements.FirstOrDefault());
        }

        public static bool IsExpressionAccessibleInScript([NotNull]ICSharpExpression expression)
        {
            var statement = expression.GetContainingStatementLike();
            if (statement == null)
                return false;

            if (statement.Parent != expression.GetContainingNode<IMethodDeclaration>()?.Body)
                return false;

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

            return true;
        }

        public static void MoveToStartWithFieldIntroduction([NotNull]IClassDeclaration classDeclaration, [NotNull]ICSharpExpression expression)
        {
            var startDeclaration = GetOrCreateStart(classDeclaration);
            MoveToMethodWithFieldIntroduction(classDeclaration, startDeclaration, expression);
        }
        
        public static void MoveToMethodWithFieldIntroduction([NotNull]IClassDeclaration classDeclaration,[NotNull] IMethodDeclaration methodDeclaration,
            [NotNull]ICSharpExpression expression)
        {
            var result = GetDeclaredElementIfDeclaration(expression);
            
            var factory = CSharpElementFactory.GetInstance(classDeclaration);

            var type = expression.Type(new ResolveContext(classDeclaration.GetPsiModule()));
            string baseName = type.GetPresentableName(CSharpLanguage.Instance);

            if (result != null)
            {
                baseName = result.ShortName;
            }
            else
            {
                if (expression is IInvocationExpression invocationExpression)
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

        private static IDeclaredElement GetDeclaredElementIfDeclaration(ICSharpExpression expression)
        {
            var localVariableDeclaration =
                LocalVariableDeclarationNavigator.GetByInitial(
                    expression.GetContainingParenthesizedExpression()?.Parent as IVariableInitializer);
            return localVariableDeclaration?.DeclaredElement;
        }
    }
}