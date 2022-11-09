using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    [QuickFix]
    public class InefficientMultidimensionalArrayUsageQuickFix : QuickFixBase
    {
        private readonly InefficientMultidimensionalArrayUsageWarning myWarning;
        private readonly IVariableDeclaration myVariableDeclaration;
        private readonly List<ITreeNodePointer<ITreeNode>> myTreeNodePointers;
        private readonly IArrayCreationExpression myArrayCreationExpression;
        private readonly IArrayType myType;
        private readonly IMultipleDeclaration myMultipleDeclaration;

        public InefficientMultidimensionalArrayUsageQuickFix(InefficientMultidimensionalArrayUsageWarning warning)
        {
            myWarning = warning;
            myVariableDeclaration = warning.VariableDeclaration;
            myTreeNodePointers = warning.Usages;
            myArrayCreationExpression = warning.ArrayCreationExpression;
            myType = (myVariableDeclaration.Type as IArrayType).NotNull("type != null");
            myMultipleDeclaration = warning.MultiplyDeclaration;
        }
        

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {

            if (!(myVariableDeclaration is ILocalVariableDeclaration localVariableDeclaration && localVariableDeclaration.TypeUsage == null))
            {
                var newType = TypeFactory.CreateArrayType(TypeFactory.CreateArrayType(myType.ElementType, 1, NullableAnnotation.NotNullable),
                    1, myType.NullableAnnotation);
                myVariableDeclaration.SetType(newType);
            }


            var factory = CSharpElementFactory.GetInstance(myVariableDeclaration);

            if (myArrayCreationExpression != null)
            {
                if (myArrayCreationExpression.Initializer == null)
                {
                    ConvertArrayCreationWithoutInitializer(factory);
                }
                else
                {
                    ConvertArrayCreation(factory);
                }
            }

            ConvertUsages(factory);

            return null;
        }

        private void ConvertArrayCreation(CSharpElementFactory factory)
        {
            var currentCreation = myArrayCreationExpression;
            var pattern = new StringBuilder("new [] {");
            var args = new List<object>();
            args.Add(myType.ElementType);

            var elements = currentCreation.ArrayInitializer.InitializerElements;
            for (int i = 0; i < elements.Count; i++)
            {
                pattern.Append($"new $0[] ${i + 1}");
                args.Add(elements[i]);
                pattern.Append(",");
            }

            pattern.Remove(pattern.Length - 1, 1);
            pattern.Append("}");

            var newCreation = factory.CreateExpression(pattern.ToString(), args.ToArray());
            currentCreation.ReplaceBy(newCreation);
        }

        private void ConvertUsages(CSharpElementFactory factory)
        {
            foreach (var usage in myTreeNodePointers)
            {
                var node = usage.GetTreeNode() as ICSharpExpression;
                var elementAccessExpression = ElementAccessExpressionNavigator.GetByOperand(node as ICSharpExpression)
                    .NotNull("ElementAccessExpressionNavigator.GetByOperand(node as ICSharpExpression) != null");
                var arguments = elementAccessExpression.Arguments;
                if (node == null || arguments.Count < 2)
                    continue;

                var newUsage = factory.CreateExpression("$0[$1][$2]", node, arguments[0], arguments[1]);
                elementAccessExpression.ReplaceBy(newUsage);
            }

        }

        private void ConvertArrayCreationWithoutInitializer(CSharpElementFactory factory)
        {
            var currentCreation = myArrayCreationExpression;

            var firstSize = currentCreation.Sizes[0].NotNull("currentCreation.Sizes[0] != null").CopyWithResolve();
            var secondSize = currentCreation.Sizes[1].NotNull("currentCreation.Sizes[1] != null").CopyWithResolve();
            
            IArrayCreationExpression newCreation;
            if (currentCreation.TypeUsage == null)
            {
                newCreation = factory.CreateExpression("new [$0][]", currentCreation.Sizes[0]) as IArrayCreationExpression;
            }
            else
            {
                newCreation = factory.CreateExpression("new $0[$1][]", currentCreation.TypeUsage, currentCreation.Sizes[0]) as IArrayCreationExpression;
            }

            Assertion.Assert(newCreation != null, "newCreation != null");
            newCreation = currentCreation.ReplaceBy(newCreation);

            if (myVariableDeclaration is ILocalVariableDeclaration)
            {
                var statement = newCreation.GetContainingStatement().NotNull("statement != null");
        
                var forInitializer = factory.CreateStatement(
                    "for (int index = 0; index < $0; index++) {$1[index] = new $2[$3];}",
                    firstSize, factory.CreateReferenceExpression(myVariableDeclaration.DeclaredName), myType.ElementType, secondSize);

                StatementUtil.InsertStatement(forInitializer, ref statement, false);
            }
        }

        public override string Text => Strings.InefficientMultidimensionalArrayUsageQuickFix_Text_Convert_to_jagged_array;
        
        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (!myWarning.IsValid())
                return false;

            // Hard to generate pretty initializer
            if (myType.Rank != 2)
                return false;
            if (myArrayCreationExpression != null && myArrayCreationExpression.Sizes.Count != 2)
                return false;

            if (myMultipleDeclaration.Declarators.Count > 1)
                return false;

            if (myVariableDeclaration is IFieldDeclaration)
                return false;

            return true;
        }
    }
}