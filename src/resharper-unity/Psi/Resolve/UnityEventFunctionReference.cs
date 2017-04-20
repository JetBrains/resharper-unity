using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    // Needs to be a void method without parameters. Can be public or private. Must be instance
    // Not a compile time error - output to log as an informational message
    // Actually doesn't care about return value
    public class UnityEventFunctionReference : CheckedReferenceBase<ILiteralExpression>, ICompletableReference, IUnityReferenceFromStringLiteral
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

            myAccessContext = new NonStaticAccessContext(myOwner);

            myMethodFilter = new InvokableMethodFilter();
            myStaticFilter = new StaticFilter(myAccessContext);
            myMethodSignatureFilter = new MethodSignatureFilter(UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_WARNING, MethodSignature);
            myUserCodeCompletionFilter = new UserCodeCompletionFilter();
        }

        public MethodSignature MethodSignature { get; }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
            if (!resolveResultWithInfo.Result.IsEmpty)
                return resolveResultWithInfo;
            return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
        }

        public override string GetName()
        {
            return myOwner.ConstantValue.Value as string ?? SharedImplUtil.MISSING_DECLARATION_NAME;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            // Just resolve to the method. ReSharper will use GetSymbolFilters to filter
            // candidates for errors
            var symbolTable =
                ResolveUtil.GetSymbolTableByTypeElement(myTargetType, SymbolTableMode.FULL, myTargetType.Module)
                    .Filter(myMethodFilter);

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            var csharpLiteral = myOwner as ICSharpLiteralExpression;
            if (csharpLiteral != null)
            {
                var range = csharpLiteral.GetStringLiteralContentTreeRange();
                if (range.Length != 0)
                    return range;
            }

            return TreeTextRange.InvalidRange;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var literalAlterer = StringLiteralAltererUtil.CreateStringLiteralByExpression(myOwner);
            literalAlterer.Replace((string)myOwner.ConstantValue.Value, element.ShortName, myOwner.GetPsiModule());
            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<UnityEventFunctionReference>() ?? this;
            return this;
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            return BindTo(element);
        }

        public override IAccessContext GetAccessContext()
        {
            return myAccessContext;
        }

        public ISymbolTable GetCompletionSymbolTable()
        {
            // Symbol table used for completion, not resolving. Show only methods from user
            // code (naively defined as anything other than classes in UnityEngine). We'll
            // still resolve even if the user types something not in this symbol table
            return GetReferenceSymbolTable(false).Filter(myMethodFilter, myUserCodeCompletionFilter);
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            // Note. Do not include the user code filter. It's not a good idea to
            // call a Unity base method, or to define your own code inside the
            // UnityEngine namespace, but let's still resolve
            return new[]
            {
                new ExactNameFilter(GetName()),
                myMethodFilter,
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