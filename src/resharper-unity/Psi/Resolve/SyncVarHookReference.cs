using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    // See https://blogs.unity3d.com/2014/05/29/unet-syncvar/
    // Make a reference from the string literal in a [SyncVar(hook = "OnValueChanged")] declaration.
    // Referenced method must be void, with one parameter equal to the type of the decorated field.
    // Can be public or private, static or instance. Unity treats an incorrect value as a compile error.
    // See also StringLiteralReferenceIncorrectSignatureError
    public class SyncVarHookReference : CheckedReferenceBase<ILiteralExpression>, ICompletableReference,
        IUnityReferenceFromStringLiteral
    {
        private readonly ITypeElement myOwningType;
        private readonly ISymbolFilter myIsMethodFilter;
        private readonly ISymbolFilter myMethodSignatureFilter;

        public SyncVarHookReference(ITypeElement owningType, IType fieldType, ILiteralExpression literal)
            : base(literal)
        {
            myOwningType = owningType;
            myIsMethodFilter = new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.METHOD);

            var methodSignature = GetMethodSignature(owningType, fieldType);
            myMethodSignatureFilter = new MethodSignatureFilter(
                UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_ERROR,
                methodSignature);
        }

        private static MethodSignature GetMethodSignature(ITypeElement owningType, IType fieldType)
        {
            var cache = owningType.GetSolution().GetComponent<IPredefinedTypeCache>();
            var predefinedType = cache.GetOrCreatePredefinedType(owningType.Module);
            var @void = predefinedType.Void;

            return new MethodSignature(@void, null, new[] {fieldType}, new[] {"value"});
        }

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
            var symbolTable = ResolveUtil.GetOwnMembersSymbolTable(myOwningType, SymbolTableMode.FULL)
                .Filter(myIsMethodFilter);

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            var cSharpLiteral = myOwner as ICSharpLiteralExpression;
            if (cSharpLiteral != null)
            {
                var range = cSharpLiteral.GetStringLiteralContentTreeRange();
                if (range.Length != 0)
                    return range;
            }

            return TreeTextRange.InvalidRange;
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var literalAlterer = StringLiteralAltererUtil.CreateStringLiteralByExpression(myOwner);
            var constantValue = (string)myOwner.ConstantValue.Value;
            Assertion.AssertNotNull(constantValue, "constantValue != null");
            literalAlterer.Replace(constantValue, element.ShortName, myOwner.GetPsiModule());
            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<SyncVarHookReference>() ?? this;
            return this;
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            return BindTo(element);
        }

        public override IAccessContext GetAccessContext()
        {
            return new DefaultAccessContext(myOwner);
        }

        public ISymbolTable GetCompletionSymbolTable()
        {
            return GetReferenceSymbolTable(false);
        }

        public override ISymbolFilter[] GetSymbolFilters()
        {
            return new[]
            {
                myIsMethodFilter,
                myMethodSignatureFilter
            };
        }
    }
}