using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using Strings = JetBrains.ReSharper.Plugins.Unity.Resources.Strings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes
{
    public abstract class AbstractMoveQuickFix : IQuickFix
    {
        [CanBeNull] protected readonly IClassDeclaration MonoBehaviourScript;
        [CanBeNull] protected readonly ICSharpExpression ToMove;
        [CanBeNull] protected readonly string FieldName;

        protected AbstractMoveQuickFix([CanBeNull] IClassDeclaration monoBehaviourScript,
                                       [CanBeNull] ICSharpExpression toMove, [CanBeNull] string fieldName = null)
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
            if (MonoBehaviourMoveUtil.IsExpressionAccessibleInMethod(ToMove, "Start"))
            {
                result.Add(new IntentionAction(new MoveAction(MonoBehaviourScript, ToMove, "Start", FieldName),
                    BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor));
            }

            if (MonoBehaviourMoveUtil.IsExpressionAccessibleInMethod(ToMove, "Awake"))
            {
                result.Add(new IntentionAction(new MoveAction(MonoBehaviourScript, ToMove, "Awake", FieldName),
                    BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor));
            }

            var loopAction = TryCreateMoveFromLoopAction(ToMove);
            if (loopAction != null)
                result.Add(loopAction);

            return result;
        }

        private IntentionAction TryCreateMoveFromLoopAction([NotNull] ICSharpExpression toMove)
        {
            ILoopStatement previousLoop = null;

            foreach (var node in toMove.ContainingNodes())
            {
                if (node is ICSharpClosure) break;
                if (node is ILoopStatement loop)
                {
                    if (MonoBehaviourMoveUtil.IsAvailableToMoveFromLoop(toMove, loop))
                        previousLoop = loop;
                    else
                        break;
                }
            }

            if (previousLoop != null)
                return new IntentionAction(new MoveFromLoopAction(toMove, previousLoop, FieldName),
                    BulbThemedIcons.ContextAction.Id, IntentionsAnchors.ContextActionsAnchor);

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

            public MoveAction([NotNull] IClassDeclaration classDeclaration, [NotNull] ICSharpExpression expression,
                              [NotNull] string methodName, [CanBeNull] string fieldName = null)
            {
                myClassDeclaration = classDeclaration;
                myExpression = expression;
                myMethodName = methodName;
                myFieldName = fieldName;
            }

            protected override Action<ITextControl> ExecutePsiTransaction(
                ISolution solution, IProgressIndicator progress)
            {
                MonoBehaviourMoveUtil.MoveToMethodWithFieldIntroduction(myClassDeclaration, myExpression, myMethodName,
                    myFieldName);
                return null;
            }

            public override string Text => string.Format(Strings.MoveAction_Text_Introduce_field_and_initialise_in___0__, myMethodName);
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
                var anchor = myLoopStatement as ICSharpStatement;

                var declaredElement = MonoBehaviourMoveUtil.GetDeclaredElementFromParentDeclaration(myToMove);
                var baseName = myVariableName ?? MonoBehaviourMoveUtil.CreateBaseName(myToMove, declaredElement);
                var name = NamingUtil.GetUniqueName(myToMove, baseName, NamedElementKinds.Locals,
                    collection => collection.Add(myToMove.Type(), new EntryOptions()),
                    de => !de.Equals(declaredElement));

                var factory = CSharpElementFactory.GetInstance(myToMove);
                var originMyToMove = myToMove.CopyWithResolve();
                MonoBehaviourMoveUtil.RenameOldUsages(myToMove, declaredElement, name, factory);

                ICSharpStatement declaration = factory.CreateStatement("var $0 = $1;", name, originMyToMove);
                StatementUtil.InsertStatement(declaration, ref anchor, true);
                return null;
            }

            public override string Text => Strings.MoveFromLoopAction_Text_Move_outside_of_loop;
        }
    }
}