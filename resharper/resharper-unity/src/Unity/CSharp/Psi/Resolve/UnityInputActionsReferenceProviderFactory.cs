using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    // from csharp method to inputactions
    [ReferenceProviderFactory]
    public class UnityInputActionsReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly ISolution mySolution;

        public UnityInputActionsReferenceProviderFactory(Lifetime lifetime, IPredefinedTypeCache predefinedTypeCache, ISolution solution)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            mySolution = solution;
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            var project = sourceFile.GetProject();
            if (project == null || !project.IsUnityProject())
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
                return new UnityInputActionsReferenceFactory(myPredefinedTypeCache, mySolution);

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }

    public class UnityInputActionsReferenceFactory : IReferenceFactory
    {
        private readonly IPredefinedTypeCache myPredefinedTypeCache;
        private readonly ISolution mySolution;

        public UnityInputActionsReferenceFactory(IPredefinedTypeCache predefinedTypeCache, ISolution solution)
        {
            myPredefinedTypeCache = predefinedTypeCache;
            mySolution = solution;
        }

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<InputActionsNameReference>(oldReferences, element))
                return oldReferences;
            
            var inputActionsElementContainer = mySolution.GetComponent<InputActionsElementContainer>();
            
            if (element is IMethodDeclaration methodDeclaration)
            {
                var usages = inputActionsElementContainer.GetUsagesFor(methodDeclaration.DeclaredElement);
                if (usages.Any()) 
                    return new ReferenceCollection(new IReference[]{new InputActionsNameReference(methodDeclaration, usages)});
            }

            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            return true;
            if (element is ILiteralExpression literal && literal.ConstantValue.IsString())
                return names.Contains(literal.ConstantValue.StringValue);
            return false;
        }
    }
    
    public class InputActionsNameReference : CheckedReferenceBase<IMethodDeclaration>
    {
        [NotNull] private readonly IMethodDeclaration myOwner;
        private readonly InputActionsDeclaredElement[] myInputActionsDeclaredElements;

        public InputActionsNameReference([NotNull] IMethodDeclaration owner, InputActionsDeclaredElement[] inputActionsDeclaredElements)
            : base(owner)
        {
            myOwner = owner;
            myInputActionsDeclaredElements = inputActionsDeclaredElements;
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            if (myInputActionsDeclaredElements.Length > 1)
            {
                return new ResolveResultWithInfo(new CandidatesResolveResult(myInputActionsDeclaredElements),
                    ResolveErrorType.MULTIPLE_CANDIDATES);
            }

            if (myInputActionsDeclaredElements.Length == 1)
                return new ResolveResultWithInfo(new SimpleResolveResult(myInputActionsDeclaredElements[0]), ResolveErrorType.OK);

            return new ResolveResultWithInfo(EmptyResolveResult.Instance,
                InputActionsResolveErrorType.UNRESOLVED_REFERENCED_INPUTACTIONS_ERROR);
        }

        public override string GetName()
        {
            return myOwner.NameIdentifier.Name;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            throw new NotImplementedException();
        }

        public override TreeTextRange GetTreeTextRange()
        {
            return myOwner.GetNameRange();
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            throw new NotImplementedException();
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            throw new NotImplementedException();
        }

        public override IAccessContext GetAccessContext()
        {
            throw new NotImplementedException();
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            throw new NotImplementedException();
        }
    }

    
}