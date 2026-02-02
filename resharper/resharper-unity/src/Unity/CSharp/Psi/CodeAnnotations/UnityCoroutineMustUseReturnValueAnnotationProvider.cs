#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeAnnotations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
internal sealed class UnityCoroutineMustUseReturnValueAnnotationProvider(UnityApi unityApi) : ICustomMustUseReturnValueAnnotationProvider
{
    public MustUseReturnValueAnnotationProvider.Requirement? GetAnnotation(IParametersOwnerWithAttributes parametersOwner)
    {
        if (parametersOwner is IMethod method && unityApi.IsUnityType(method.ContainingType))
        {
            var predefinedType = method.Module.GetPredefinedType();
            if (method.ReturnType.Equals(predefinedType.IEnumerator))
            {
                return new MustUseReturnValueAnnotationProvider.Requirement("Coroutine will not continue if return value is ignored");
            }
        }

        return null;
    }
}