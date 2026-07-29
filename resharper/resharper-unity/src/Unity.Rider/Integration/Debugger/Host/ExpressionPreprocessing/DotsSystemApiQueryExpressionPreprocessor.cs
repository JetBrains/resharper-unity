using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Backend.Features.Debugger.ExpressionPreprocessing;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Debugger.Host.ExpressionPreprocessing
{
    /// <summary>
    /// Makes <c>SystemAPI.Query</c> evaluable in the debugger (RIDER-102087).
    /// <para>
    /// The whole chain is source-generator scaffolding declared as
    /// <c>throw InternalCompilerInterface.ThrowCodeGenException()</c>, so it always throws as written. Running on
    /// the sandbox document (see <c>DebuggerSandboxUtils</c>) gives the PSI context to lower it onto the real
    /// query API the generator emits at build time:
    /// </para>
    /// <code><![CDATA[
    /// SystemAPI.Query<RefRW<LocalTransform>>().WithNone<RotationSpeed>()
    /// ->
    /// state.GetEntityQuery(new Unity.Entities.EntityQueryDesc {
    ///     All = new Unity.Entities.ComponentType[] { Unity.Entities.ComponentType.ReadOnly<Unity.Transforms.LocalTransform>() },
    ///     None = new Unity.Entities.ComponentType[] { Unity.Entities.ComponentType.ReadOnly<RotationSpeed>() }
    /// }).ToEntityArray(Unity.Collections.Allocator.Temp)
    /// ]]></code>
    /// <para>
    /// <c>EntityQueryDesc</c> is used rather than the <c>ref struct</c> <c>EntityQueryBuilder</c>, whose return
    /// value cannot cross the interpreter boundary. Unsupported chains are left to throw as written.
    /// </para>
    /// </summary>
    [Language(typeof(CSharpLanguage))]
    public class DotsSystemApiQueryExpressionPreprocessor : IExpressionPreprocessor
    {
        private const string EntityQueryDescFullName = "Unity.Entities.EntityQueryDesc";
        private const string ComponentTypeFullName = "Unity.Entities.ComponentType";
        private const string TempAllocator = "Unity.Collections.Allocator.Temp";

        private const string QueryMethod = "Query";
        private const string WithAllMethod = "WithAll";
        private const string WithAnyMethod = "WithAny";
        private const string WithNoneMethod = "WithNone";
        private const string WithEntityAccessMethod = "WithEntityAccess";

        [CanBeNull]
        public string PreprocessExpression(string expression, IFile file, DocumentRange range)
        {
            if (file is not ICSharpFile)
                return null;

            // Registered for every C# solution (the bundled Unity plugin is zone-gated to the debugger zone, not
            // to Unity solutions), so bail before doing PSI work in ordinary .NET projects.
            if (!file.GetSolution().HasUnityReference())
                return null;

            var chain = FindQueryChain(file.GetSolution(), range);
            if (chain == null)
                return null;

            // The rewrite replaces the whole evaluated expression, so the chain must *be* exactly that expression.
            // If the range is larger (a trailing `.Length`) or smaller (a sub-selection), leave it to evaluate
            // (and fail) as written rather than answer for a different expression.
            if (chain[chain.Count - 1].GetDocumentRange().TextRange != range.TextRange)
                return null;

            // Query type arguments are the required components; they must be `RefRO`/`RefRW` wrappers (aspects and
            // other parameters don't map to a single component type), otherwise leave the expression alone.
            var all = new List<string>();
            foreach (var typeArgument in chain[0].TypeArguments)
            {
                var component = GetComponentClrName(typeArgument, allowDirect: false);
                if (component == null)
                    return null;
                all.Add(component);
            }

            // `all` is non-empty here: IsSystemApiQuery guarantees a type argument and any unmappable one returned above.
            var any = new List<string>();
            var none = new List<string>();
            for (var i = 1; i < chain.Count; i++)
            {
                if (!CollectModifier(chain[i], all, any, none))
                    return null;
            }

            // Build the query against the owning system. `ISystem.OnUpdate` takes it as a `ref SystemState` -
            // `state` by convention, but not enforced.
            var systemState = FindSystemStateParameterName(chain[0]);
            if (systemState == null)
                return null;

            var builder = new StringBuilder(systemState);
            builder.Append(".GetEntityQuery(new ").Append(EntityQueryDescFullName).Append(" { ");
            builder.Append("All = ").Append(ComponentTypeArray(all));
            if (any.Count > 0)
                builder.Append(", Any = ").Append(ComponentTypeArray(any));
            if (none.Count > 0)
                builder.Append(", None = ").Append(ComponentTypeArray(none));
            builder.Append(" }).ToEntityArray(").Append(TempAllocator).Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Finds the <c>SystemAPI.Query</c> invocation chain at <paramref name="range"/> and returns its
        /// invocations ordered from the query call to the outermost modifier, or null when the expression is not
        /// a <c>SystemAPI.Query</c> chain.
        /// </summary>
        [CanBeNull]
        private static List<IInvocationExpression> FindQueryChain(ISolution solution, DocumentRange range)
        {
            foreach (var invocation in TextControlToPsi.GetElements<IInvocationExpression>(solution, range.StartOffset))
            {
                var chain = GetOrderedChain(GetOutermostInvocation(invocation));

                // Cheap syntactic gate before the resolving IsSystemApiQuery: skip the resolve for the far more
                // common non-`Query` generic invocations.
                if ((chain[0].InvokedExpression as IReferenceExpression)?.NameIdentifier?.Name != QueryMethod)
                    continue;

                if (chain[0].IsSystemApiQuery())
                    return chain;
            }

            return null;
        }

        private static IInvocationExpression GetOutermostInvocation(IInvocationExpression invocation)
        {
            var result = invocation;
            while (true)
            {
                var reference = ReferenceExpressionNavigator.GetByQualifierExpression(result);
                var parent = InvocationExpressionNavigator.GetByInvokedExpression(reference);
                if (parent == null)
                    return result;
                result = parent;
            }
        }

        private static List<IInvocationExpression> GetOrderedChain(IInvocationExpression outermost)
        {
            var chain = new List<IInvocationExpression>();
            for (var current = outermost;
                 current != null;
                 current = (current.InvokedExpression as IReferenceExpression)?.QualifierExpression as IInvocationExpression)
            {
                chain.Add(current);
            }

            chain.Reverse();
            return chain;
        }

        /// <summary>
        /// Collects one <c>WithAll</c>/<c>WithAny</c>/<c>WithNone</c>/<c>WithEntityAccess</c> link. Returns false
        /// as soon as it sees a modifier this lowering cannot express, so that the whole expression is left alone.
        /// </summary>
        private static bool CollectModifier(IInvocationExpression modifier, List<string> all, List<string> any, List<string> none)
        {
            switch ((modifier.InvokedExpression as IReferenceExpression)?.NameIdentifier?.Name)
            {
                // Only reshapes the yielded tuple; the entities it adds are what this lowering already returns.
                case WithEntityAccessMethod:
                    return true;
                case WithAllMethod:
                    return CollectComponents(modifier, all);
                case WithAnyMethod:
                    return CollectComponents(modifier, any);
                case WithNoneMethod:
                    return CollectComponents(modifier, none);
                default:
                    return false;
            }
        }

        private static bool CollectComponents(IInvocationExpression modifier, List<string> target)
        {
            if (modifier.TypeArguments.Count == 0)
                return false;

            foreach (var typeArgument in modifier.TypeArguments)
            {
                // These name component types directly, but accept a `RefRO`/`RefRW` wrapper too.
                var component = GetComponentClrName(typeArgument, allowDirect: true);
                if (component == null)
                    return false;
                target.Add(component);
            }

            return true;
        }

        /// <summary>
        /// Unwraps the component out of <c>RefRO</c>/<c>RefRW</c>/<c>EnabledRefRO</c>/<c>EnabledRefRW</c>. When
        /// <paramref name="allowDirect"/> is set a plain component type is accepted as-is; otherwise a non-wrapper
        /// type argument yields null so the caller can bail out.
        /// </summary>
        [CanBeNull]
        private static string GetComponentClrName(IType type, bool allowDirect)
        {
            if (type.GetScalarType() is not { } declaredType)
                return null;

            // GetClrName is null for an unresolved/error type; bail rather than dereferencing it below.
            var clrName = declaredType.GetClrName();

            if (clrName.Equals(KnownTypes.RefRO) || clrName.Equals(KnownTypes.RefRW) ||
                clrName.Equals(KnownTypes.EnabledRefRO) || clrName.Equals(KnownTypes.EnabledRefRW))
            {
                var typeElement = declaredType.GetTypeElement();
                if (typeElement is not { TypeParametersCount: 1 })
                    return null;

                var component = declaredType.GetSubstitution()[typeElement.TypeParameters[0]];
                return ToEvaluableTypeName(component.GetScalarType()?.GetClrName());
            }

            return allowDirect ? ToEvaluableTypeName(clrName) : null;
        }

        /// <summary>
        /// Turns a CLR type name into one that can be pasted into the C# expression the debugger compiles. A nested
        /// type uses <c>+</c> in its CLR name but <c>.</c> in source; a generic type cannot be an
        /// <c>IComponentData</c> and its <c>`1</c> arity is not valid C#, so bail on it.
        /// </summary>
        [CanBeNull]
        private static string ToEvaluableTypeName([CanBeNull] IClrTypeName clrName)
        {
            var fullName = clrName?.FullName;
            if (fullName == null || fullName.Contains('`'))
                return null;

            return fullName.Replace('+', '.');
        }

        [CanBeNull]
        private static string FindSystemStateParameterName(IInvocationExpression invocation)
        {
            var method = invocation.GetContainingNode<IMethodDeclaration>()?.DeclaredElement;
            if (method == null)
                return null;

            foreach (var parameter in method.Parameters)
            {
                if (parameter.Type.GetScalarType()?.GetClrName()?.Equals(KnownTypes.SystemState) == true &&
                    !string.IsNullOrEmpty(parameter.ShortName))
                    return parameter.ShortName;
            }

            return null;
        }

        private static string ComponentTypeArray(IReadOnlyList<string> components)
        {
            var builder = new StringBuilder("new ").Append(ComponentTypeFullName).Append("[] { ");
            for (var i = 0; i < components.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(ComponentTypeFullName).Append(".ReadOnly<").Append(components[i]).Append(">()");
            }

            return builder.Append(" }").ToString();
        }
    }
}
