using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    // See https://blogs.unity3d.com/2014/05/29/unet-syncvar/
    // Make a reference from the string literal in a [SyncVar(hook = "OnValueChanged")] declaration.
    // Referenced method must be void, with one parameter equal to the type of the decorated field.
    // Can be public or private, static or instance. Unity treats an incorrect value as a compile error.
    // See also StringLiteralReferenceIncorrectSignatureError
    public class SyncVarHookReference : StringLiteralReferenceBase, ICompletableReference
    {
        private readonly ITypeElement myOwningType;
        private readonly ISymbolFilter myIsMethodFilter;
        private readonly ISymbolFilter myMethodSignatureFilter;

        public SyncVarHookReference(ITypeElement owningType, IType fieldType, ILiteralExpression literal)
            : base(literal)
        {
            myOwningType = owningType;
            myIsMethodFilter = new DeclaredElementTypeFilter(ResolveErrorType.NOT_RESOLVED, CLRDeclaredElementType.METHOD);

            MethodSignature = GetMethodSignature(owningType, fieldType);
            myMethodSignatureFilter = new MethodSignatureFilter(
                UnityResolveErrorType.UNITY_STRING_LITERAL_REFERENCE_INCORRECT_SIGNATURE_ERROR,
                MethodSignature);
        }

        public MethodSignature MethodSignature { get; }

        private static MethodSignature GetMethodSignature(ITypeElement owningType, IType fieldType)
        {
            var cache = owningType.GetSolution().GetComponent<IPredefinedTypeCache>();
            var predefinedType = cache.GetOrCreatePredefinedType(owningType.Module);
            var @void = predefinedType.Void;

            return new MethodSignature(@void, null, new[] {fieldType}, new[] {"value"});
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            // This symbol table is used for both resolve and completion, so provide all the candidates (all methods)
            // here. The filters from GetSymbolFilters will be applied during resolve, and GetCompletionSymbolTable can
            // apply its own if it needs to
            var symbolTable = ResolveUtil.GetOwnMembersSymbolTable(myOwningType, SymbolTableMode.FULL)
                .Filter(myIsMethodFilter);

            if (useReferenceName)
            {
                var name = GetName();
                return symbolTable.Filter(name, new ExactNameFilter(name));
            }
            return symbolTable;
        }

        // Applied to GetReferenceSymbolTable() during resolve
        public override ISymbolFilter[] GetSymbolFilters()
        {
            return new[] {myMethodSignatureFilter};
        }

        public ISymbolTable GetCompletionSymbolTable()
        {
            // No filters, just show all methods. We'll show a resolve error if the method signatures don't match
            return GetReferenceSymbolTable(false);
        }
    }
}