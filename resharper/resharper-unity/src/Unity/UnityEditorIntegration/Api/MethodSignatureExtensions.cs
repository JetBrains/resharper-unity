using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public static class MethodSignatureExtensions
    {
        public static MethodSignature AsMethodSignature(this UnityEventFunction eventFunction,
                                                        KnownTypesCache knownTypesCache, IPsiModule module)
        {
            var returnType = eventFunction.ReturnType.AsIType(knownTypesCache, module);

            if (eventFunction.Parameters.Length == 0)
                return new MethodSignature(returnType, eventFunction.IsStatic);

            var parameterTypes = new IType[eventFunction.Parameters.Length];
            var parameterNames = new string[eventFunction.Parameters.Length];
            for (var i = 0; i < eventFunction.Parameters.Length; i++)
            {
                var parameter = eventFunction.Parameters[i];
                var paramType = parameter.TypeSpec.AsIType(knownTypesCache, module);
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
                    if (!DoTypesMatch(method.Parameters[i].Type, eventFunction.Parameters[i].TypeSpec))
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
            return DoTypesMatch(method.ReturnType, eventFunction.ReturnType)
                   || (eventFunction.CanBeCoroutine && IsEnumerator(method.ReturnType));
        }

        private static bool DoTypesMatch(IType type, UnityTypeSpec typeSpec)
        {
            // TODO: Replace with Equals(type, typeSpec.AsIType) if this gets more complex
            // This handles the types we currently have to deal with - scalars, simple arrays and simple generics. This
            // method is called frequently, so use KnownTypesCache to mitigate creating too many instances of IType for
            // the type spec
            if (typeSpec.IsArray != type is IArrayType)
                return false;

            // This doesn't handle an array of generic types, but that's ok
            if (type is IArrayType arrayType)
                return Equals(arrayType.ElementType.GetTypeElement()?.GetClrName(), typeSpec.ClrTypeName);

            var typeElement = type.GetTypeElement();
            if (typeElement == null)
                return false;

            if (Equals(typeElement.GetClrName(), typeSpec.ClrTypeName))
            {
                // Now check generics
                if (typeElement.TypeParameters.Count != typeSpec.TypeParameters.Length)
                    return false;

                if (typeSpec.TypeParameters.Length > 0)
                {
                    var substitution = (type as IDeclaredType)?.GetSubstitution();
                    if (substitution == null)
                        return false;

                    var i = 0;
                    foreach (var typeParameter in substitution.Domain)
                    {
                        var typeParameterType = substitution[typeParameter];
                        if (!Equals(typeParameterType.GetTypeElement()?.GetClrName(), typeSpec.TypeParameters[i]))
                            return false;
                        i++;
                    }
                }
                return true;
            }

            return false;
        }

        private static bool IsEnumerator(IType type)
        {
            return type is IDeclaredType declaredType && Equals(declaredType.GetClrName(), PredefinedType.IENUMERATOR_FQN);
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