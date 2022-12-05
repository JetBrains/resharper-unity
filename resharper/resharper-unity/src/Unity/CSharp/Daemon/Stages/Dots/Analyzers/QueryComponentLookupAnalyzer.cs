#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots.Analyzers
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration))]
    public class QueryComponentLookupAnalyzer : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        public QueryComponentLookupAnalyzer(UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            var isDotsImplicitlyUsedType = UnityApi.IsDerivesFromISystem(typeElement);
            if(!isDotsImplicitlyUsedType)
                return;

            var queryLookupFields = new HashSet<IDeclaredElement>();
            var dictionary = new Dictionary<IDeclaredElement, IFieldDeclaration>();

            foreach (var elementClassMemberDeclaration in element.ClassMemberDeclarations)
            {
                if (elementClassMemberDeclaration is IFieldDeclaration fieldDeclaration)
                {
                    var fieldDeclarationDeclaredElement = fieldDeclaration.DeclaredElement;
                    
                    var isComponentLookup = fieldDeclarationDeclaredElement != null && UnityApi.IsComponentLookup(fieldDeclarationDeclaredElement.Type.GetTypeElement());

                    if (isComponentLookup)
                    {
                        queryLookupFields.Add(fieldDeclarationDeclaredElement);
                        dictionary.Add(fieldDeclarationDeclaredElement, fieldDeclaration);
                    }
                }
            }
            
            if(queryLookupFields.Count == 0)
                return;
            
            var methodDeclarations = typeElement.GetDeclarations().OfType<IStructDeclaration>()
                .Where(d => !d.GetSourceFile().IsSourceGeneratedFile()).SelectMany(d => d.MethodDeclarations)
                .ToArray();

            if (methodDeclarations.Length > 0) //TODO:strange situation, no methods 0_o
            {
                foreach (var methodDeclaration in methodDeclarations)
                {
                    var method = methodDeclaration.DeclaredElement;
                    if(UnityApi.IsISystemOnCreateMethod(method))
                        continue;
                    if(UnityApi.IsISystemOnDestroyMethod(method))
                        continue;
                
                    methodDeclaration.ProcessThisAndDescendants(new VisitorDotsMethods(queryLookupFields));
                }
            }
            
            foreach (var field in queryLookupFields)
                consumer.AddHighlighting(new NotUpdatedComponentLookupWarning(dictionary[field], field.ShortName));
        }
    }

    public class VisitorDotsMethods : IRecursiveElementProcessor
    {
        private readonly HashSet<IDeclaredElement> myQueryLookupFields;

        public VisitorDotsMethods(HashSet<IDeclaredElement> queryLookupFields)
        {
            myQueryLookupFields = queryLookupFields;
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            if (element is ILocalFunctionDeclaration)
                return false;
            if (element is ILambdaExpression)
                return false;

            return true;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            if(myQueryLookupFields.Count == 0)
                return;
            
            if(element is not IInvocationExpression expression)
                return;

            var method = expression.Reference.Resolve().DeclaredElement as IMethod;
            
            if(method == null)
                return;
            
            //looking for .Update(ref SystemState state) method
            
            if(method.ShortName != "Update")
                return;
            
            if(!UnityApi.IsComponentLookup(method.ContainingType))
                return;
            
            if(method.Parameters.Count != 1)
                return;

            var possibleStateParameter = method.Parameters[0]; 
            if(!possibleStateParameter.IsRefMember())
                return;
            
            if(!UnityApi.IsSystemStateType(possibleStateParameter.Type.GetTypeElement()))
                return;

            var qualifier = expression.InvocationExpressionReference.IsPassThrough() 
                ? expression.GetInvokedReferenceExpressionQualifier() 
                : expression.ConditionalQualifier;
            
            if(qualifier is not IReferenceExpression possibleFieldReference)
                return;

            var possibleFieldElement = possibleFieldReference.Reference.Resolve().DeclaredElement;

            if (myQueryLookupFields.Contains(possibleFieldElement))
                myQueryLookupFields.Remove(possibleFieldElement);

            ProcessingIsFinished = myQueryLookupFields.Count == 0;
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished { get; private set; }
    }
}