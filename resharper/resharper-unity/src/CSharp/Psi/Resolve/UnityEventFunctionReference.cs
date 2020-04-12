using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    // Needs to be a void method without parameters. Can be public or private. Must be instance
    // Not a compile time error - output to log as an informational message
    // Actually doesn't care about return value
    public class UnityEventFunctionReference : StringLiteralReferenceBase, ICompletableReference
    {
        private readonly ITypeElement myTargetType;
        private readonly IAccessContext myAccessContext;
        private readonly ISymbolFilter myMethodFilter;
        private readonly ISymbolFilter myStaticFilter;
        private readonly ISymbolFilter myMethodSignatureFilter;
        private readonly ISymbolFilter myUserCodeCompletionFilter;

        // e.g. literalExpressionOwner = "InvokeRepeating"
        // targetType = "MyMonoBehaviour"
        public UnityEventFunctionReference(ITypeElement targetType, ILiteralExpression literal, MethodSignature methodSignature)
            : base(literal)
        {
            myTargetType = targetType;
            MethodSignature = methodSignature;

            // All Unity event functions are instance methods, but handle the method signature
            if (methodSignature.IsStatic == null)
                myAccessContext = new DefaultAccessContext(myOwner);
            else if (methodSignature.IsStatic == true)
                myAccessContext = new StaticAccessContext(myOwner);
            else
                myAccessContext = new NonStaticAccessContext(myOwner);

            myMethodFilter = new InvokableMethodFilter();
            myStaticFilter = new StaticFilter(myAccessContext);
            myMethodSignatureFilter = new MethodSignatureFilter(UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_WARNING, MethodSignature);
            myUserCodeCompletionFilter = new UserCodeCompletionFilter();
        }

        public MethodSignature MethodSignature { get; }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            if (!myTargetType.IsValid())
                return EmptySymbolTable.INSTANCE;

            // This symbol table is used for both resolve and completion. Return all possible candidates, and then
            // filter appropriately for resolve (via GetSymbolFilters) and completion (via GetCompletionSymbolTable).
            // Resolve will have stricter filtering - it's better for completion to show ALL methods and then show a
            // resolve error for invalid signature than to confuse the user with missing methods in completion.
            var symbolTable = ResolveUtil
                .GetSymbolTableByTypeElement(myTargetType, SymbolTableMode.FULL, myTargetType.Module)
                .Filter(myMethodFilter);

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override IAccessContext GetAccessContext() => myAccessContext;

        public ISymbolTable GetCompletionSymbolTable()
        {
            // Completion should show all methods, even if the signatures are incorrect. It's better to accept an
            // invalid method and show a resolve error that allows the user to fix the signature than it is to not show
            // the method in completion at all. The only filter we do apply is to hide non-user code methods, to remove
            // methods that we're not likely to call. We'll still resolve them though.
            return GetReferenceSymbolTable(false).Filter(myUserCodeCompletionFilter);
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            // Note that we don't include the user code filter here - it's only for completion. It's not illegal to call
            // Unity base class methods, but unlikely, so let's not clutter up completion with it.
            return new[]
            {
                myStaticFilter,
                myMethodSignatureFilter
            };
        }

        private class InvokableMethodFilter : SimpleSymbolFilter
        {
            public override ResolveErrorType ErrorType => ResolveErrorType.NOT_RESOLVED;

            public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
            {
                return declaredElement is IMethod && !(declaredElement is IAccessor) &&
                       !CSharpDeclaredElementUtil.IsDestructor(declaredElement);
            }
        }

        private class UserCodeCompletionFilter : SimpleSymbolFilter
        {
            public override ResolveErrorType ErrorType => ResolveErrorType.NOT_RESOLVED;

            public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
            {
                if (declaredElement is IMethod method)
                {
                    var containingType = method.GetContainingType();
                    if (containingType == null)
                        return true;

                    return !containingType.IsObjectClass() && containingType.GetContainingNamespace().QualifiedName != "UnityEngine";
                }
                return false;
            }
        }
    }
}