using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis
{
    public static class BurstCodeAnalysisUtil
    {
        private static readonly IClrTypeName[] ourFixedStrings =
        {
            new ClrTypeName("Unity.Collections.FixedString32"),
            new ClrTypeName("Unity.Collections.FixedString64"),
            new ClrTypeName("Unity.Collections.FixedString128"),
            new ClrTypeName("Unity.Collections.FixedString512"),
            new ClrTypeName("Unity.Collections.FixedString4096")
        };
        
        public const string BURST_DISPLAY_NAME = BURST_TOOLTIP;
        public const string BURST_TOOLTIP = "Burst compiled code";

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

            foreach (var fixedString in ourFixedStrings)
            {
                if (clrTypeName.Equals(fixedString))
                    return true;
            }

            return false;
        }

        [ContractAnnotation("null => false")]
        public static bool IsBurstPossibleArgumentString([NotNull] ICSharpArgument argument)
        {
            var unused = argument.Expression; // for debug
            var expressionType = argument.GetExpressionType();
            var type = expressionType.ToIType();
            
            // if expression is type A -> then everything that returns from it is A. 
            // if in burst context there are managed variables(like string) -> it will be highlighted
            // assume there are no
            // then there are only IStringLiteralOwner, fixedString and IInvocationExpression
            return type.IsString() || IsFixedString(type);
        }

        [ContractAnnotation("null => false")]
        public static bool IsDebugLog([CanBeNull] IMethod method)
        {
            if (method == null)
                return false;

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

        [ContractAnnotation("null => false")]
        public static bool IsStringFormat([CanBeNull] IMethod method)
        {
            if (method == null)
                return false;

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

        public static bool IsBurstCompileFunctionPointer([CanBeNull] IMethod method)
        {
            if (method == null)
                return false;
            
            var isContainingTypeBurstCompile =
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.BurstCompiler) ?? false;

            return isContainingTypeBurstCompile && method.ShortName == "CompileFunctionPointer";
        }

        public static bool IsBurstProhibitedObjectMethod([NotNull] IMethod method)
        {
            var containingType = method.GetContainingType();

            // GetHashCode permitted in burst only if no boxing happens i.e. calling base.GetHashCode
            // Equals is prohibited because it works through System.Object and require boxing, which 
            // Burst does not support
            if (containingType is IStruct && method.IsOverridesObjectGetHashCode())
                return false;

            // it means object method is called, only GetHashCode allowed
            if (containingType is IStruct && method.IsOverride)
                return true;
            
            // NOTE: this method checks that it is EXACTLY object/valueType method, 
            // for common inheritance it would return false!
            return containingType is IClass @class &&
                   (@class.IsSystemValueTypeClass() || @class.IsObjectClass());
        }

        public static bool HasBurstProhibitedReturnValue([NotNull] IParametersOwner invokedMethod)
        {
            return invokedMethod.ReturnType.Classify == TypeClassification.REFERENCE_TYPE;
        }

        public static bool HasBurstProhibitedArguments([NotNull] IArgumentList argumentList)
        {
            foreach (var argument in argumentList.Arguments)
            {
                var matchingParameterType = argument.MatchingParameter?.Type;

                if (matchingParameterType != null && !IsBurstPermittedType(matchingParameterType))
                    return true;
            }

            return false;
        }

        public static bool IsBurstProhibitedNode([NotNull] ITreeNode node)
        {
            switch (node)
            {
                case IThrowStatement _:
                case IThrowExpression _:
                case IInvocationExpression invocationExpression
                    when invocationExpression.Reference.Resolve().DeclaredElement is IMethod method &&
                         IsBurstDiscarded(method):
                case IFunctionDeclaration functionDeclaration
                    when functionDeclaration.DeclaredElement is IFunction function &&
                         IsBurstProhibitedFunction(function):
                case IAttributeSectionList _:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBurstProhibitedFunction([NotNull] IFunction function)
        {
            if (!IsBurstPossibleFunction(function))
                //it is consistent with method semantics
                return true;

            return function is IMethod method && IsBurstDiscarded(method);
        }

        public static bool IsBurstPossibleFunction([NotNull] IFunction function)
        {
            return function.IsStatic || function.GetContainingTypeMember() is IStruct;
        }

        public static bool IsBurstDiscarded([NotNull] IMethod method)
        {
            return method.HasAttributeInstance(KnownTypes.BurstDiscardAttribute, AttributesSource.Self);
        }

        [ContractAnnotation("null => false")]
        public static bool IsSharedStaticCreateMethod([CanBeNull] IMethod method)
        {
            var containingType = method?.GetContainingType();
            var typeClrName = containingType?.GetClrName();

            if (typeClrName == null)
                return false;

            if (!typeClrName.Equals(KnownTypes.SharedStatic))
                return false;

            if (method.IsStatic == false)
                return false;

            if (method.ShortName != "GetOrCreate")
                return false;

            return true;
        }
    }
}