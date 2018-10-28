using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    public abstract class AbstractMoveQuickFix : IQuickFix
    {
        [CanBeNull] protected readonly IClassDeclaration MonoBehaviourScript;
        [CanBeNull] protected readonly ICSharpExpression ToMove;
        [CanBeNull] protected readonly string FieldName;

        public AbstractMoveQuickFix([CanBeNull] IClassDeclaration monoBehaviourScript, [CanBeNull]ICSharpExpression toMove, [CanBeNull] string fieldName = null)
        {
            MonoBehaviourScript = monoBehaviourScript;
            ToMove = toMove;
            FieldName = fieldName;
        }
        
        public virtual IEnumerable<IntentionAction> CreateBulbItems()
        {
            Assertion.Assert(ToMove != null, "ToMove != null");
            Assertion.Assert(MonoBehaviourScript != null, "MonoBehaviourScript != null");

            var result = new List<IntentionAction>();
            if (MonoBehaviourMoveUtil.IsExpressionAccessibleInScript(ToMove))
            {
                result.Add(new IntentionAction(new MoveAction(MonoBehaviourScript, ToMove, "Start"), BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor ));
                result.Add(new IntentionAction(new MoveAction(MonoBehaviourScript, ToMove, "Awake"), BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor ));
            }

            var loopAction = TryCreateMoveFromLoopAction(ToMove);
            if (loopAction != null)
                result.Add(loopAction);

            return result;
        }

        private IntentionAction TryCreateMoveFromLoopAction([NotNull] ICSharpExpression toMove)
        {
            ITreeNode currentExpression = toMove;
            ILoopStatement previousLoop = null;
            while (true)
            {
                var loop = currentExpression.GetContainingNode<ILoopStatement>();
                if (loop == null)
                    break;

                if (MonoBehaviourMoveUtil.IsAvailableToMoveFromScope(toMove, loop, previousLoop))
                {
                    previousLoop = loop;
                    currentExpression = loop;
                }
                else
                {
                    break;
                }
            }

            if (previousLoop != null)
            {
                return new IntentionAction(new MoveFromLoopAction(ToMove, previousLoop), BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor);
            }
            
            return null;
        }

        public virtual bool IsAvailable(IUserDataHolder cache)
        {
            if (MonoBehaviourScript == null || ToMove == null)
                return false;
            if (!MonoBehaviourScript.IsValid() || !ToMove.IsValid())
                return false;
            return true;
        }
        
        public class MoveAction : BulbActionBase
        {
            [NotNull] private readonly IClassDeclaration myClassDeclaration;
            [NotNull] private readonly ICSharpExpression myExpression;
            [NotNull] private readonly string myMethodName;
            [CanBeNull] private readonly string myFieldName;

            public MoveAction([NotNull] IClassDeclaration classDeclaration, [NotNull] ICSharpExpression expression,[NotNull] string methodName, [CanBeNull] string fieldName = null)
            {
                myClassDeclaration = classDeclaration;
                myExpression = expression;
                myMethodName = methodName;
                myFieldName = fieldName;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                MonoBehaviourMoveUtil.MoveToMethodWithFieldIntroduction(myClassDeclaration, myExpression, myMethodName, myFieldName);
                return null;
            }

            public override string Text => $"Move to {myMethodName}";
        }
        
        public class MoveFromLoopAction : BulbActionBase
        {
            private readonly ICSharpExpression myToMove;
            private readonly ILoopStatement myLoopStatement;
            [CanBeNull] private readonly string myVariableName;

            public MoveFromLoopAction([NotNull] ICSharpExpression toMove, [NotNull] ILoopStatement loopStatement, [CanBeNull] string variableName = null)
            {
                myToMove = toMove;
                myLoopStatement = loopStatement;
                myVariableName = variableName;
            }
            
            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var statement = myToMove.GetContainingStatement();
                Assertion.AssertNotNull(statement, "statement != null");
                var anchor = myLoopStatement as ICSharpStatement;

                var declaredElement = MonoBehaviourMoveUtil.GetDeclaredElementFromParentDeclaration(myToMove);
                var baseName = myVariableName ?? MonoBehaviourMoveUtil.CreateBaseName(myToMove, declaredElement);
                var name = NamingUtil.GetUniqueName(myToMove, baseName, NamedElementKinds.Locals, de => !de.Equals(declaredElement));
                
                var factory = CSharpElementFactory.GetInstance(myToMove);
                
                ICSharpStatement declaration = factory.CreateStatement("var $0 = $1;", name, myToMove.Copy());
                StatementUtil.InsertStatement(declaration, ref anchor, true);
                myToMove.ReplaceBy(factory.CreateReferenceExpression(name));
                
                statement.RemoveOrReplaceByEmptyStatement();
                return null;
            }

            public override string Text => "Move outside the loop";
        }
    }
}