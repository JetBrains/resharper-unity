using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public class MethodSignature
    {
        public MethodSignature(IType returnType)
            : this(returnType, EmptyArray<IType>.Instance)
        {
        }

        public MethodSignature(IType returnType, params IType[] parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public IType ReturnType { get; }
        public IType[] ParameterTypes { get; }
    }

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

            if (myMethodSignature.ParameterTypes.Length != method.Parameters.Count)
                return false;

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                if (!Equals(myMethodSignature.ParameterTypes[i], method.Parameters[i].Type))
                {
                    ITypeConversionRule rule = method.Module.GetTypeConversionRule();
                    if (!rule.IsImplicitlyConvertibleTo(method.Parameters[i].Type, myMethodSignature.ParameterTypes[i]))
                        return false;
                }
            }

            return true;
        }
    }
}