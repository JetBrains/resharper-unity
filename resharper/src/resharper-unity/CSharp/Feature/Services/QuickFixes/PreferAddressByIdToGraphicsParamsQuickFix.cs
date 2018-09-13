using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferAddressByIdToGraphicsParamsQuickFix : QuickFixBase
    {
        private IInvocationExpression myInvocationExpression;
        private ICSharpArgument myArgument;
        private string myLiteral;
        private string myMapFuntion;
        private string myTypeName; 
        
        public PreferAddressByIdToGraphicsParamsQuickFix(PreferAddressByIdToGraphicsParamsWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myArgument = warning.Argument;
            myLiteral = warning.Literal;
            myMapFuntion = warning.MapFunction;
            myTypeName = warning.TypeName;
        }
        
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var psiModule = myInvocationExpression.PsiModule;
            var classDeclaration = GetTopLevelClass(myInvocationExpression);

            var factory = CSharpElementFactory.GetInstance(myInvocationExpression);

            var fieldInitializerValue = factory.CreateExpression("$0.$1($2)", 
                TypeFactory.CreateTypeByCLRName(myTypeName, myInvocationExpression.PsiModule),
                myMapFuntion,
                $"\"{myLiteral}\"");


            // generate field if we need it. If we have field which addresses to same property reuse it.
            var name = TryFindField(myLiteral, classDeclaration, myTypeName, myMapFuntion);

            if (name == null)
            {
                name = GetUniqueName(myInvocationExpression, myLiteral);
                var newMemberDeclaration = factory.CreateFieldDeclaration(psiModule.GetPredefinedType().Int, name);

                newMemberDeclaration.SetReadonly(true);
                newMemberDeclaration.SetStatic(true);
                newMemberDeclaration.SetAccessRights(AccessRights.PRIVATE);
                newMemberDeclaration.SetInitial(factory.CreateExpressionInitializer(fieldInitializerValue));

                classDeclaration.AddClassMemberDeclaration(newMemberDeclaration);
            }

            // replace argument
            var argument = factory.CreateArgument(ParameterKind.VALUE, factory.CreateReferenceExpression(name));
            myArgument.ReplaceBy(argument);
            return null;
        }

        private static string TryFindField(string propertyName, IClassDeclaration declaration, string requiredTypeName, string requiredMethodName)
        {
            foreach (var field in declaration.FieldDeclarations)
            {
                var initializer = field.Initial;
                if (initializer is IExpressionInitializer exprInit && exprInit.Value is IInvocationExpression invocation)
                {
                    var method = invocation.Reference?.Resolve()?.DeclaredElement as IMethod;
                    
                    if (method == null) continue;
                    if (!method.ShortName.Equals(requiredMethodName)) continue;

                    var containingType = method.GetContainingType();
                    if (containingType == null) continue;

                    if (!containingType.GetClrName().FullName.Equals(requiredTypeName)) continue;

                    var arguments = invocation.Arguments;

                    if (arguments.Count == 1 &&
                        arguments[0].Expression is ILiteralExpression literal &&
                        literal.ConstantValue.Value is string str &&
                        str.Equals(propertyName))
                    {
                        return field.DeclaredName;
                    }
                }
            }

            return null;
        }
        
        private static string GetUniqueName(IInvocationExpression invocationExpression, string baseName)
        {
            var namingManager = invocationExpression.GetPsiServices().Naming;
            var policyProvider = namingManager.Policy.GetPolicyProvider(invocationExpression.Language, invocationExpression.GetSourceFile());
            var namingRule = policyProvider.GetPolicy(NamedElementKinds.PrivateStaticReadonly).NamingRule;
            var name = namingManager.Parsing.Parse(baseName, namingRule, policyProvider);
            var nameRoot = name.GetRootOrDefault(baseName);
            var namesCollection = namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, CSharpLanguage.Instance, true, policyProvider);
            namesCollection.Add(nameRoot, new EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, emphasis: Emphasis.Good));
            var suggestionOptions = new SuggestionOptions
            {
                DefaultName = baseName,
                UniqueNameContext = invocationExpression,
            };
            var namesSuggestion = namesCollection.Prepare(NamedElementKinds.PrivateStaticReadonly, ScopeKind.Common, suggestionOptions);
            return namesSuggestion.FirstName();
        }

        private static IClassDeclaration GetTopLevelClass(IInvocationExpression expression)
        {

            var baseClass = expression.GetContainingNode<IClassDeclaration>();
            if (baseClass == null)
            {
                return null;
            }

            var next = ClassDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            while (next!= null)
            {
                baseClass = next;
                next = ClassDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            }

            return baseClass;
        }
        
        
        public override string Text => "Use int overload";
        
        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myArgument.IsValid();
        }
    }
}