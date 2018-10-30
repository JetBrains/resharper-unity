using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class MethodSignatureExtensions
    {
        private static readonly IClrTypeName ourEnumeratorType = new ClrTypeName("System.Collections.IEnumerator");

        public static MethodSignature AsMethodSignature(this UnityEventFunction eventFunction, IPsiModule module)
        {
            IType returnType = TypeFactory.CreateTypeByCLRName(eventFunction.ReturnType, module);
            if (eventFunction.ReturnTypeIsArray)
                returnType = TypeFactory.CreateArrayType(returnType, 1);

            if (eventFunction.Parameters.Length == 0)
                return new MethodSignature(returnType, eventFunction.IsStatic);

            var parameterTypes = new IType[eventFunction.Parameters.Length];
            var parameterNames = new string[eventFunction.Parameters.Length];
            for (var i = 0; i < eventFunction.Parameters.Length; i++)
            {
                var parameter = eventFunction.Parameters[i];
                IType paramType = TypeFactory.CreateTypeByCLRName(parameter.ClrTypeName, module);
                if (parameter.IsArray)
                    paramType = TypeFactory.CreateArrayType(paramType, 1);
                parameterTypes[i] = paramType;
                parameterNames[i] = parameter.Name;
            }

            return new MethodSignature(returnType, eventFunction.IsStatic, parameterTypes, parameterNames);
        }

        public static MethodSignatureMatch Match(this UnityEventFunction eventFunction, [NotNull] IMethod method)
        {
            if (method.ShortName != eventFunction.Name)
                return MethodSignatureMatch.NoMatch;

            var match = MethodSignatureMatch.ExactMatch;
            if (method.IsStatic != eventFunction.IsStatic)
                match |= MethodSignatureMatch.IncorrectStaticModifier;
            if (!HasMatchingParameters(eventFunction, method))
                match |= MethodSignatureMatch.IncorrectParameters;
            if (!HasMatchingReturnType(eventFunction, method))
                match |= MethodSignatureMatch.IncorrectReturnType;
            if (!HasMatchingTypeParameters(method))
                match |= MethodSignatureMatch.IncorrectTypeParameters;
            return match;
        }

        // Note that this assumes the methods are the same, i.e. names already match
        public static MethodSignatureMatch Match(this MethodSignature methodSignature,
            [NotNull] IMethodDeclaration methodDeclaration)
        {
            var match = MethodSignatureMatch.ExactMatch;
            if (methodSignature.IsStatic.HasValue && methodDeclaration.IsStatic != methodSignature.IsStatic)
                match |= MethodSignatureMatch.IncorrectStaticModifier;
            if (!HasMatchingParameters(methodSignature, methodDeclaration))
                match |= MethodSignatureMatch.IncorrectParameters;
            if (!Equals(methodSignature.ReturnType, methodDeclaration.Type))
                match |= MethodSignatureMatch.IncorrectReturnType;
            if (!HasMatchingTypeParameters(methodDeclaration))
                match |= MethodSignatureMatch.IncorrectTypeParameters;
            return match;
        }

        private static bool HasMatchingParameters(MethodSignature methodSignature, IMethodDeclaration methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterDeclarations;
            if (methodSignature.Parameters.Length != parameters.Count)
                return false;

            for (var i = 0; i < parameters.Count; i++)
            {
                if (!Equals(methodSignature.Parameters[i].Type, parameters[i].Type))
                    return false;
            }

            return true;
        }

        private static bool HasMatchingParameters(UnityEventFunction eventFunction, IMethod method)
        {
            var matchingParameters = false;
            if (method.Parameters.Count == eventFunction.Parameters.Length)
            {
                matchingParameters = true;
                for (var i = 0; i < eventFunction.Parameters.Length && matchingParameters; i++)
                {
                    if (!DoTypesMatch(method.Parameters[i].Type, eventFunction.Parameters[i].ClrTypeName,
                        eventFunction.Parameters[i].IsArray))
                    {
                        matchingParameters = false;
                    }
                }
            }
            else
            {
                // TODO: This doesn't really handle optional parameters very well
                // It's fine for the current usage (a single parameter, either there or not)
                // but won't work for anything more interesting. Perhaps optional parameters
                // should be modeled as overloads?
                var optionalParameters = 0;
                foreach (var parameter in eventFunction.Parameters)
                {
                    if (parameter.IsOptional)
                        optionalParameters++;
                }
                if (method.Parameters.Count + optionalParameters == eventFunction.Parameters.Length)
                    matchingParameters = true;
            }

            return matchingParameters;
        }

        private static bool HasMatchingReturnType(UnityEventFunction eventFunction, IMethod method)
        {
            return DoTypesMatch(method.ReturnType, eventFunction.ReturnType, eventFunction.ReturnTypeIsArray)
                   || (eventFunction.Coroutine && DoTypesMatch(method.ReturnType, ourEnumeratorType, false));
        }

        private static bool DoTypesMatch(IType type, IClrTypeName expectedTypeName, bool isArray)
        {
            IDeclaredType declaredType;

            if (type is IArrayType arrayType)
            {
                if (!isArray) return false;

                // TODO: Does this handle multi-dimensional arrays? Do we care?
                declaredType = arrayType.GetScalarType();
            }
            else
            {
                declaredType = (IDeclaredType) type;
            }

            return declaredType != null && Equals(declaredType.GetClrName(), expectedTypeName);
        }

        private static bool HasMatchingTypeParameters(IMethodDeclaration methodDeclaration)
        {
            // There aren't any generic Unity methods, so we only match if there are none
            return methodDeclaration.TypeParameterDeclarations.Count == 0;
        }

        private static bool HasMatchingTypeParameters(IMethod method)
        {
            // There aren't any generic Unity methods, so we only match if there are none
            return method.TypeParameters.Count == 0;
        }
    }
}