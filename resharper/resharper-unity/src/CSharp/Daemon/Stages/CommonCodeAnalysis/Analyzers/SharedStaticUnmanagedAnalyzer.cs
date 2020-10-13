using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CommonCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class SharedStaticUnmanagedAnalyzer : CommonProblemAnalyzerBase<IInvocationExpression>
    {
        protected override void Analyze(IInvocationExpression invocationExpression, IDaemonProcess daemonProcess,
            DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;
            
            if (!BurstCodeAnalysisUtil.IsSharedStaticCreateMethod(invokedMethod))
                return;
            
            var substitution = invocationExpression.Reference.Resolve().Substitution;
            var domain = substitution.Domain;
            var sharedStaticDomain = domain.Where(parameter =>
                    parameter.OwnerType is IStruct @struct && @struct.GetClrName().Equals(KnownTypes.SharedStatic))
                .ToList();

            //CGTD can this be 0? 
            Assertion.Assert(sharedStaticDomain.Count == 1, "SharedStatic should have 1 substitution");

            if (sharedStaticDomain.Count != 1)
                return;

            var typeParameter = sharedStaticDomain[0];

            // it means Burst finally supported type parameter unmanaged constraint
            Assertion.Assert(!typeParameter.IsUnmanagedType, "SharedStatic doesn't have unmanaged constraint");

            if (!typeParameter.IsValid() || typeParameter.IsUnmanagedType)
                return;

            var substitutedType = substitution.Apply(typeParameter);

            if (substitutedType.IsUnmanagedType(invocationExpression.GetLanguageVersion()))
                return;

            var typeParameterName = substitutedType.GetTypeElement()?.ShortName;

            if (typeParameterName != null)
                consumer.AddHighlighting(new SharedStaticUnmanagedTypeWarning(invocationExpression, typeParameterName));
        }
    }
}