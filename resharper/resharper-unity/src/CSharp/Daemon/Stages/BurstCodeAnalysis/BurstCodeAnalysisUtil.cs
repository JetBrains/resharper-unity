using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis
{
    public static class BurstCodeAnalysisUtil
    {
        private static readonly IClrTypeName[] FixedStrings =
        {
            new ClrTypeName("Unity.Collections.FixedString32"),
            new ClrTypeName("Unity.Collections.FixedString64"),
            new ClrTypeName("Unity.Collections.FixedString128"),
            new ClrTypeName("Unity.Collections.FixedString512"),
            new ClrTypeName("Unity.Collections.FixedString4096")
        };

        [ContractAnnotation("null => false")]
        public static bool IsBurstPermittedType([CanBeNull] IType type)
        {
            if (type == null)
                return false;

            // this construction only to simplify debugging, just place breakpoint to appropriate switch
            switch (type)
            {
                case IType _ when type.IsValueType():
                case IType _ when type.IsStructType():
                case IType _ when type.IsPredefinedNumeric():
                case IType _ when type.IsEnumType():
                case IType _ when type.IsVoid():
                case IType _ when type.IsIntPtr():
                case IType _ when type.IsUIntPtr():
                case IType _ when type.IsPointerType():
                case IType _ when type.IsOpenType:
                case IType _ when IsFixedString(type):
                    return true;
                default:
                    return false;
            }
        }

        [ContractAnnotation("null => false")]
        public static bool IsFixedString([CanBeNull] IType type)
        {
            var declaredType = type as IDeclaredType;
            
            if (declaredType == null)
                return false;

            var clrTypeName = declaredType.GetClrName();
            
            foreach (var fixedString in FixedStrings)
            {
                if (clrTypeName.Equals(fixedString))
                    return true;
            }

            return false;
        }

        [ContractAnnotation("null => false")]
        public static bool IsBurstPossibleArgumentString(IType type)
        {
            // if expression is type A -> then everything that returns form it is A. 
            // if in burst context there are managed variables(like string) -> it will be highlighted
            // assume there are no
            // then there are only IStringLiteralOwners, fixedStrings and IInvocatoinExressions

            return type.IsString() || IsFixedString(type);
        }

        [ContractAnnotation("null => false")]
        private static bool IsAccessedFromOpenType(
            [CanBeNull] IConditionalAccessExpression conditionalAccessExpression)
        {
            if (conditionalAccessExpression is IInvocationExpression)
            {
                conditionalAccessExpression =
                    conditionalAccessExpression.ConditionalQualifier as IConditionalAccessExpression;
            }

            if (conditionalAccessExpression == null)
                return false;

            return conditionalAccessExpression.ConditionalQualifier?.Type().IsOpenType ?? false;
        }

        public static bool IsDebugLog([NotNull] IMethod method)
        {
            if (!method.IsStatic)
                return false;

            if (method.Parameters.Count != 1)
                return false;

            if (method.ShortName != "Log" && method.ShortName != "LogError" && method.ShortName != "LogWarning")
                return false;

            var clrTypeName = method.GetContainingType()?.GetClrName();
            
            if (clrTypeName == null)
                return false;

            if (!clrTypeName.Equals(KnownTypes.Debug))
                return false;

            var parameter = method.Parameters[0];
            
            if (!parameter.Type.IsObject())
                return false;

            return true;
        }

        public static bool IsStringFormat([NotNull] IMethod method)
        {
            if (!method.IsStatic)
                return false;

            if (method.ShortName != "Format")
                return false;

            var clrTypeName = method.GetContainingType()?.GetClrName();
            
            if (clrTypeName == null)
                return false;

            if (!clrTypeName.Equals(PredefinedType.STRING_FQN))
                return false;

            var parameters = method.Parameters;
            if (parameters.Count < 2)
                return false;

            if (!parameters[0].Type.IsString())
                return false;

            return true;
        }

        public static bool IsBurstCompileFunctionPointerMethod([NotNull] IMethod method)
        {
            var isContainingTypeBurstCompile =
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.BurstCompiler) ?? false;

            return isContainingTypeBurstCompile && method.ShortName == "CompileFunctionPointer";
        }

        public static bool IsBurstProhibitedObjectMethodInvocation([CanBeNull] IInvocationExpression invocation)
        {
            var function =
                invocation?.InvocationExpressionReference.Resolve().DeclaredElement as IFunction;
            
            if (function == null)
                return false;
            
            var containingType = function.GetContainingType();
            
            // GetHashCode permitted in burst only if no boxing happens i.e. calling base.GetHashCode
            // Equals is prohibited because it works through System.Object and require boxing, which 
            // Burst does not support
            if (containingType is IStruct && function is IMethod method && method.IsOverridesObjectGetHashCode())
                return false;

            var isValueTypeOrObject = containingType is IClass @class &&
                                      (@class.IsSystemValueTypeClass() || @class.IsObjectClass());
            
            return isValueTypeOrObject || containingType is IStruct && function.IsOverride;
        }

        [ContractAnnotation("null => false")]
        public static bool IsReturnValueBurstProhibited([CanBeNull] IFunction invokedMethod)
        {
            if (invokedMethod == null)
                return false;

            return invokedMethod.ReturnType.Classify == TypeClassification.REFERENCE_TYPE;
        }

        [ContractAnnotation("null => false")]
        public static bool HasBurstProhibitedArguments([CanBeNull] IArgumentList argumentList)
        {
            if (argumentList == null)
                return false;

            foreach (var argument in argumentList.Arguments)
            {
                var matchingParameterType = argument.MatchingParameter?.Type;
                
                if (matchingParameterType != null && !IsBurstPermittedType(matchingParameterType))
                    return true;
            }

            return false;
        }

        public static bool IsBurstContextBannedNode(ITreeNode node)
        {
            switch (node)
            {
                case IThrowStatement _:
                case IThrowExpression _:
                case IInvocationExpression invocationExpression
                    when CallGraphUtil.GetCallee(invocationExpression) is IMethod method && IsBurstDiscarded(method):
                case IFunctionDeclaration functionDeclaration
                    when IsBurstContextBannedFunction(functionDeclaration.DeclaredElement):
                case IAttributeSectionList _:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBurstContextBannedFunction(IFunction function)
        {
            if (function == null)
                //true because it is consistent with method semantics
                return true;
            
            if (function.IsStatic || function.GetContainingTypeMember() is IStruct)
                return function is IMethod method && IsBurstDiscarded(method);
            
            return true;
        }

        private static bool IsBurstDiscarded(IMethod method)
        {
            var attributes = method.GetAttributeInstances(KnownTypes.BurstDiscardAttribute, AttributesSource.Self);

            return attributes.Count != 0;
        }
    }
}