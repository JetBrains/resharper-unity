using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions
{
    public abstract class AddInspectorAttributeAction : IContextAction
    {
        [NotNull] protected static readonly SubmenuAnchor ourBaseAnchor = 
            new SubmenuAnchor(IntentionsAnchors.ContextActionsAnchor, SubmenuBehavior.Static("Modify Inspector attribute"));

        protected readonly ICSharpContextActionDataProvider DataProvider;
        private readonly IAnchor myAnchor;

        public AddInspectorAttributeAction(ICSharpContextActionDataProvider dataProvider, IAnchor anchor)
        {
            DataProvider = dataProvider;
            myAnchor = anchor;
        }
        
        protected abstract IClrTypeName AttributeTypeName { get; }
        protected virtual bool IsRemoveActionAvailable() => false;
        
        public abstract BulbActionBase GetActionForOne(IMultipleFieldDeclaration multipleFieldDeclaration, IFieldDeclaration fieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute);
        public abstract BulbActionBase GetActionForAll(IMultipleFieldDeclaration multipleFieldDeclaration, IPsiModule module,
            CSharpElementFactory elementFactory, IAttribute existingAttribute);

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var fieldDeclaration = DataProvider.GetSelectedElement<IFieldDeclaration>();
            var multipleFieldDeclaration = MultipleFieldDeclarationNavigator.GetByDeclarator(fieldDeclaration);
            var unityApi = DataProvider.Solution.GetComponent<UnityApi>();
            
            if (!unityApi.IsSerialisedField(fieldDeclaration?.DeclaredElement) || multipleFieldDeclaration == null)
                return EmptyList<IntentionAction>.Enumerable;

            var existingAttribute = AttributeUtil.GetAttribute(fieldDeclaration, AttributeTypeName);

            if (multipleFieldDeclaration.Declarators.Count == 1)
            {

                return new[]
                {
                    GetActionForOne(multipleFieldDeclaration, fieldDeclaration, DataProvider.PsiModule,DataProvider.ElementFactory, existingAttribute).ToContextActionIntention(myAnchor)
                };
            }
            
            return new[]
            {
                GetActionForOne(multipleFieldDeclaration, fieldDeclaration, DataProvider.PsiModule, DataProvider.ElementFactory, existingAttribute).ToContextActionIntention(myAnchor),
                GetActionForAll(multipleFieldDeclaration, DataProvider.PsiModule, DataProvider.ElementFactory, existingAttribute).ToContextActionIntention(myAnchor)
            };
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            if (!DataProvider.Project.IsUnityProject())
                return false;

            var unityApi = DataProvider.Solution.GetComponent<UnityApi>();
            var fieldDeclaration = DataProvider.GetSelectedElement<IFieldDeclaration>();
            if (!unityApi.IsSerialisedField(fieldDeclaration?.DeclaredElement))
                return false;
            
            var existingAttribute = AttributeUtil.GetAttribute(fieldDeclaration, AttributeTypeName);
            if (existingAttribute != null && !IsRemoveActionAvailable())
                return false;
            
            var classDeclaration = fieldDeclaration.GetContainingTypeDeclaration();
            var classElement = classDeclaration?.DeclaredElement;
            if (classElement == null)
                return false;

            
            return unityApi.IsDescendantOfMonoBehaviour(classElement) ||
                   unityApi.IsDescendantOfScriptableObject(classElement);
        }
        
        public static void AddAttribute(IClrTypeName attributeClrTypeName, IFieldDeclaration fieldDeclaration, AttributeValue[] values, IPsiModule module)
        {
            var elementFactory = CSharpElementFactory.GetInstance(module);
            var attributeType = TypeFactory.CreateTypeByCLRName(attributeClrTypeName, module);
            var attributeTypeElement = attributeType.GetTypeElement();
            if (attributeTypeElement == null)
                return;

            var attribute =  elementFactory.CreateAttribute(attributeTypeElement, values, EmptyArray<Pair<string, AttributeValue>>.Instance);
            
            
            CSharpSharedImplUtil.AddAttributeAfter(fieldDeclaration, attribute, null);
        }
    }
}