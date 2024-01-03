#nullable enable

using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class PreferAddressByIdToGraphicsParamsQuickFix : QuickFixBase
    {
        private readonly IInvocationExpression myInvocationExpression;
        private readonly ICSharpArgument myArgument;

        // using for name of new generated field
        private readonly string myFieldName;

        // using for find already declared field or property
        private readonly string myGraphicsPropertyName;

        // using to handle concatenation operations with const values and save possibility to correct refactoring
        private readonly IExpression myArgumentExpression;
        private readonly string myMapFunction;
        private readonly string myTypeName;

        public PreferAddressByIdToGraphicsParamsQuickFix(PreferAddressByIdToGraphicsParamsWarning warning)
        {
            myInvocationExpression = warning.InvocationMethod;
            myArgument = warning.Argument;
            myFieldName = myGraphicsPropertyName = warning.Literal;

            if (!ValidityChecker.IsValidIdentifier(myFieldName)) myFieldName = "Property";

            myArgumentExpression = warning.ArgumentExpression;
            myMapFunction = warning.MapFunction;
            myTypeName = warning.TypeName;
        }

        protected override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = CSharpElementFactory.GetInstance(myInvocationExpression);

            //  try find declaration where string to id conversation is done. If we don't find it, create in top-level class
            var idDeclaration = TryFindDeclaration(myInvocationExpression, myGraphicsPropertyName, myTypeName, myMapFunction);
            if (idDeclaration == null)
            {
                var psiModule = myInvocationExpression.PsiModule;
                var fieldInitializerValue = factory.CreateExpression("$0.$1($2)",
                    TypeFactory.CreateTypeByCLRName(myTypeName, myInvocationExpression.PsiModule),
                    myMapFunction,
                    myArgumentExpression.Copy());

                var name = NamingUtil.GetUniqueName( myInvocationExpression, myFieldName, NamedElementKinds.PrivateStaticReadonly).NotNull();
                var newDeclaration = factory.CreateFieldDeclaration(psiModule.GetPredefinedType().Int, name);
                idDeclaration = newDeclaration.DeclaredElement;
                if (idDeclaration == null)
                    return null;

                newDeclaration.SetReadonly(true);
                newDeclaration.SetStatic(true);
                newDeclaration.SetAccessRights(AccessRights.PRIVATE);
                newDeclaration.SetInitial(factory.CreateExpressionInitializer(fieldInitializerValue));

                // TODO: [C#8] default interface implementations
                // interface is not good place to add field declaration
                var classDeclaration = GetTopLevelClassLikeDeclaration(myInvocationExpression);
                classDeclaration?.AddClassMemberDeclaration(newDeclaration);
            }

            // replace argument
            var referenceExpression = factory.CreateReferenceExpression("$0", idDeclaration);

            var argument = factory.CreateArgument(ParameterKind.VALUE, myArgument.NameIdentifier?.Name, referenceExpression);
            myArgument.ReplaceBy(argument);
            return null;
        }

        private static IDeclaredElement? TryFindIdDeclarationInClassLikeDeclaration(IClassLikeDeclaration declaration, string propertyName,
            string requiredTypeName, string requiredMethodName)
        {
            var classDeclaredElement = declaration.DeclaredElement;
            if (classDeclaredElement == null)
                return null;

            var members = classDeclaredElement.GetMembers().Where(x => x is IField or IProperty)
                .SelectNotNull(x => x.GetDeclarations().SingleOrDefault());

            foreach (var member in members)
            {
                switch (member)
                {
                    case IFieldDeclaration field:
                        if (HandleField(field, propertyName, requiredTypeName, requiredMethodName))
                            return field.DeclaredElement;
                        break;
                    case IPropertyDeclaration property:
                        if (HandleProperty(property, propertyName, requiredTypeName, requiredMethodName))
                        {
                            return property.DeclaredElement;
                        }
                        break;
                }
            }

            return null;
        }

        private static bool HandleProperty(IPropertyDeclaration property, string propertyName, string requiredTypeName, string requiredMethodName)
        {
            if (!property.IsStatic)
                return false;

            var accessors = property.AccessorDeclarations;
            if (accessors.Count != 1)
                return false;

            if (accessors[0].Kind != AccessorKind.GETTER)
                return false;

            if (property.DeclaredElement == null)
                return false;

            var initializer = property.Initial;
            return initializer is IExpressionInitializer { Value: IInvocationExpression invocation } &&
                   HandleInvocation(invocation, propertyName, requiredTypeName, requiredMethodName);
        }

        private static bool HandleField(IFieldDeclaration field, string propertyName, string requiredTypeName, string requiredMethodName)
        {
            if (!field.IsStatic || !field.IsReadonly)
                return false;

            if (field.DeclaredElement == null)
                return false;

            var initializer = field.Initial;
            return initializer is IExpressionInitializer { Value: IInvocationExpression invocation } &&
                   HandleInvocation(invocation, propertyName, requiredTypeName, requiredMethodName);
        }

        private static bool HandleInvocation(IInvocationExpression invocation, string propertyName, string requiredTypeName,
            string requiredMethodName)
        {
            var method = invocation.Reference.Resolve().DeclaredElement as IMethod;

            if (method == null) return false;
            if (!method.ShortName.Equals(requiredMethodName)) return false;

            var containingType = method.ContainingType;
            if (containingType == null) return false;

            if (!containingType.GetClrName().FullName.Equals(requiredTypeName)) return false;

            var arguments = invocation.Arguments;

            if (arguments.Count == 1 && arguments[0].Value != null)
            {
                var constantValue = arguments[0].Value.ConstantValue(new UniversalContext(invocation));
                if (constantValue.IsString(out var stringValue) && stringValue?.Equals(propertyName) == true)
                    return true;
            }

            return false;
        }

        private static IDeclaredElement? TryFindDeclaration(IInvocationExpression expression, string propertyName,
                                                            string requiredTypeName, string requiredMethodName)
        {
            var baseClass = expression.GetContainingNode<IClassLikeDeclaration>();
            if (baseClass == null)
            {
                return null;
            }

            var result = TryFindIdDeclarationInClassLikeDeclaration(baseClass, propertyName, requiredTypeName, requiredMethodName);
            if (result != null)
                return result;

            var next = ClassLikeDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            while (next != null)
            {
                baseClass = next;
                result = TryFindIdDeclarationInClassLikeDeclaration(baseClass, propertyName, requiredTypeName, requiredMethodName);
                if (result != null)
                    return result;

                next = ClassLikeDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            }

            return null;
        }

        private static IClassLikeDeclaration? GetTopLevelClassLikeDeclaration(IInvocationExpression expression)
        {
            var baseClass = expression.GetContainingNode<IClassLikeDeclaration>();
            if (baseClass == null)
                return null;

            var next = ClassLikeDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            while (next != null)
            {
                baseClass = next;
                next = ClassLikeDeclarationNavigator.GetByNestedTypeDeclaration(baseClass);
            }

            return baseClass;
        }

        // TODO: Show different text
        // "Introduce field to cache property index"
        // "Use cached property index"
        public override string Text => Strings.PreferAddressByIdToGraphicsParamsQuickFix_Text_Use_cached_property_index;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            return myInvocationExpression.IsValid() && myArgument.IsValid();
        }
    }
}
