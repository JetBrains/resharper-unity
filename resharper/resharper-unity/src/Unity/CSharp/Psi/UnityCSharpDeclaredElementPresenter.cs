using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi
{
    [PsiSharedComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityCSharpDeclaredElementPresenter : CSharpDeclaredElementPresenter
    {
        public override string GetEntityKind(IDeclaredElement declaredElement)
        {
            if (declaredElement.IsFromUnityProject())
            {
                var unityApi = declaredElement.GetSolution().GetComponent<UnityApi>();
                switch (declaredElement)
                {
                    case IMethod method when unityApi.IsEventFunction(method):
                        return "event function";
                    case IField field when unityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.OdinSerializedField):
                        return "odin serialised field";
                    case IField field when unityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.SerializedField):
                        return "serialised field";
                }
            }
            return base.GetEntityKind(declaredElement);
        }
    }
}