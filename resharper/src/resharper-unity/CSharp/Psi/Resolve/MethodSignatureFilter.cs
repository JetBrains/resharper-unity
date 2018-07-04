using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class MethodSignatureFilter : SimpleSymbolFilter
    {
        private readonly MethodSignature myMethodSignature;

        public MethodSignatureFilter(ResolveErrorType errorType, MethodSignature methodSignature)
        {
            myMethodSignature = methodSignature;
            ErrorType = errorType;
        }

        public override ResolveErrorType ErrorType { get; }

        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            var method = declaredElement as IMethod;
            if (method == null)
                return false;

            if (!Equals(myMethodSignature.ReturnType, method.ReturnType))
                return false;

            if (myMethodSignature.Parameters.Length != method.Parameters.Count)
                return false;

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                if (!Equals(myMethodSignature.Parameters[i].Type, method.Parameters[i].Type))
                {
                    ITypeConversionRule rule = method.Module.GetTypeConversionRule();
                    if (!rule.IsImplicitlyConvertibleTo(method.Parameters[i].Type, myMethodSignature.Parameters[i].Type))
                        return false;
                }
            }

            return true;
        }
    }
}