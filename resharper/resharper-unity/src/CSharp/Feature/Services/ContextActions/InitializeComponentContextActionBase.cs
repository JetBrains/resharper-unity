using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
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
        Name = "Initialize field in Start or Awake method",
        Description =
            "Initializes current field in Start or Awake method via calling `GetComponent`")]
    public class InitializeFieldComponentContextAction : InitializeComponentContextActionBase<IFieldDeclaration>
    {
        public InitializeFieldComponentContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
    }
    
    [ContextAction(Group = UnityContextActions.GroupID,
        Name = "Initialize property in Start or Awake method",
        Description =
            "Initializes current property in Start or Awake method via calling `GetComponent`")]
    public class InitializePropertyComponentContextAction : InitializeComponentContextActionBase<IPropertyDeclaration>
    {
        public InitializePropertyComponentContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
    }
    
    public abstract class InitializeComponentContextActionBase<T> : IContextAction where T : class, ITypeOwnerDeclaration
    {
        // TODO : 2 separate actions or nested?
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        [NotNull] private static readonly SubmenuAnchor ourSubmenuAnchor2 =
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Executable);
        
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public InitializeComponentContextActionBase(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var typeOwner = myDataProvider.GetSelectedElement<T>();
            Assertion.Assert(typeOwner != null, "typeOwner != null");
            var classDeclaration = typeOwner.GetContainingNode<IClassDeclaration>();
            Assertion.Assert(classDeclaration != null, "classDeclaration != null");

            return new[]
            {
                new InitializeComponentBulbActionBase(typeOwner.DeclaredName, typeOwner.Type.GetTypeElement(),
                    classDeclaration, "Start").ToContextActionIntention(ourSubmenuAnchor),
                new InitializeComponentBulbActionBase(typeOwner.DeclaredName, typeOwner.Type.GetTypeElement(),
                    classDeclaration, "Awake").ToContextActionIntention(ourSubmenuAnchor2),
            };
        }
        
        public virtual bool IsAvailable(IUserDataHolder cache)
        {
            var typeOwner = myDataProvider.GetSelectedElement<T>();
            var type = typeOwner?.Type.GetTypeElement();
            if (type == null || !type.IsUnityComponent(out _))
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

            public override string Text => $"Initialize at {myMethodName}";
        }
    }
}