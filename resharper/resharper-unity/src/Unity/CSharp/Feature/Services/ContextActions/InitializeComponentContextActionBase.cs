using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.InitializeFieldComponentContextAction_Name), 
        DescriptionResourceName = nameof(Strings.InitializeFieldComponentContextAction_Description))]
    public class InitializeFieldComponentContextAction : InitializeComponentContextActionBase<IFieldDeclaration>
    {
        public InitializeFieldComponentContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
    }

    [ContextAction(Group = UnityContextActions.GroupID,
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.InitializePropertyComponentContextAction_Name), 
        DescriptionResourceName = nameof(Strings.InitializePropertyComponentContextAction_Description))]
    public class InitializePropertyComponentContextAction : InitializeComponentContextActionBase<IPropertyDeclaration>
    {
        public InitializePropertyComponentContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
    }

    public abstract class InitializeComponentContextActionBase<T> : IContextAction where T : class, ITypeOwnerDeclaration
    {
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        [NotNull] private static readonly SubmenuAnchor ourAttributeSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);

        private readonly ICSharpContextActionDataProvider myDataProvider;

        protected InitializeComponentContextActionBase(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var typeOwner = myDataProvider.GetSelectedElement<T>();
            Assertion.Assert(typeOwner != null, "typeOwner != null");
            var classDeclaration = typeOwner.GetContainingNode<IClassDeclaration>();
            Assertion.Assert(classDeclaration != null, "classDeclaration != null");

            var type = typeOwner.Type;

            if (!HasRequireComponentWithSameType(type, classDeclaration))
                yield return new AddRequireComponentBulbActionBase(type, classDeclaration)
                    .ToContextActionIntention(ourAttributeSubmenuAnchor);


            if (!IsInitializedIn(classDeclaration, typeOwner.DeclaredElement, "Start") &&
                !IsInitializedIn(classDeclaration, typeOwner.DeclaredElement, "Awake"))
            {
                yield return new InitializeComponentBulbActionBase(typeOwner.DeclaredName, type.GetTypeElement(),
                    classDeclaration, "Start").ToContextActionIntention(ourSubmenuAnchor);
                yield return new InitializeComponentBulbActionBase(typeOwner.DeclaredName, type.GetTypeElement(),
                    classDeclaration, "Awake").ToContextActionIntention(ourSubmenuAnchor);
            }
        }

        private bool IsInitializedIn(IClassDeclaration classDeclaration, IDeclaredElement typeOwnerDeclaredElement, string methodName)
        {
            var method = MonoBehaviourMoveUtil.GetMonoBehaviourMethod(classDeclaration, methodName);
            if (method == null)
                return false;

            foreach (var assignmentExpression in method.Descendants<IAssignmentExpression>())
            {
                if (assignmentExpression.Dest is IReferenceExpression referenceExpression)
                {
                    var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
                    if (typeOwnerDeclaredElement.Equals(declaredElement))
                        return true;
                }
            }

            return false;
        }

        private bool HasRequireComponentWithSameType(IType type, IClassDeclaration classDeclaration)
        {
            var existingAttributes = AttributeUtil.GetAttributes(classDeclaration, KnownTypes.RequireComponent);
            foreach (var attribute in existingAttributes)
            {
                var argument = attribute.Arguments.FirstOrDefault();

                if (argument?.Value is ITypeofExpression typeofExpression)
                {
                    if (typeofExpression.ArgumentType.Equals(type))
                        return true;
                }
            }

            return false;
        }

        public virtual bool IsAvailable(IUserDataHolder cache)
        {
            if (!(myDataProvider.GetSelectedElement<IClassLikeDeclaration>() is IClassDeclaration))
                return false;

            var selectedField = myDataProvider.GetSelectedElement<T>() as IFieldDeclaration;
            if (selectedField == null)
                return false;

            var fieldType = selectedField.Type.GetTypeElement();
            if (fieldType == null)
                return false;

            if (!fieldType.IsUnityComponent(out _))
                return false;

            var ownerType = selectedField.DeclaredElement?.ContainingType;
            if (ownerType == null)
                return false;

            if (!ownerType.IsUnityComponent(out _))
                return false;

            return true;
        }

        private class InitializeComponentBulbActionBase : BulbActionBase
        {
            private readonly string myName;
            private readonly ITypeElement myTypeElement;
            private readonly IClassDeclaration myClassDeclaration;
            private readonly string myMethodName;
            private readonly CSharpElementFactory myFactory;

            public InitializeComponentBulbActionBase(string name, ITypeElement typeElement,
                IClassDeclaration classDeclaration, string methodName)
            {
                myName = name;
                myTypeElement = typeElement;
                myClassDeclaration = classDeclaration;
                myMethodName = methodName;
                myFactory = CSharpElementFactory.GetInstance(classDeclaration);
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                var method = MonoBehaviourMoveUtil.GetOrCreateMethod(myClassDeclaration, myMethodName);
                var statement = myFactory.CreateStatement("$0 = GetComponent<$1>();", myName, myTypeElement);
                var body = method.EnsureStatementMemberBody();
                body.AddStatementBefore(statement, body.Statements.FirstOrDefault());
                return null;
            }

            public override string Text => string.Format(Strings.InitializeComponentBulbActionBase_Text_Initialize_in___0__, myMethodName);
        }

        private class AddRequireComponentBulbActionBase : BulbActionBase
        {
            private readonly IType myType;
            private readonly IClassDeclaration myClassDeclaration;
            private readonly CSharpElementFactory myFactory;

            public AddRequireComponentBulbActionBase(IType type, IClassDeclaration classDeclaration)
            {
                myType = type;
                myClassDeclaration = classDeclaration;
                myFactory = CSharpElementFactory.GetInstance(classDeclaration);
            }

            protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
            {
                AttributeUtil.AddAttributeToSingleDeclaration(myClassDeclaration, KnownTypes.RequireComponent,
                    new[] {new AttributeValue(myType)}, null, myClassDeclaration.GetPsiModule(), myFactory, true);
                return null;
            }

            public override string Text => Strings.AddRequireComponentBulbActionBase_Text_Add__RequireComponent_;
        }
    }
}