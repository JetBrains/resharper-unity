#nullable enable
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.CodeStyle.Naming;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Naming.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeStyle.Naming
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabCppNamingPolicyProvider : ICustomCppNamingPolicyProvider
    {
        private const string InputStructName = "Input"; 
        
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public ShaderLabCppNamingPolicyProvider(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }

        public NamingPolicy? TryGetNamingPolicy(IDeclaredElement declaredElement, IPsiSourceFile? sourceFile, IContextBoundSettingsStore? settingsStore)
        {
            if (myUnitySolutionTracker.IsUnityProjectOrHasUnityReference)
            {
                if (declaredElement.GetElementType() == CppDeclaredElementTypes.STRUCT && declaredElement.ShortName.Equals(InputStructName))
                    return NamingPolicy.Default;
            }

            return null;
        }

        public NamingPolicy? TryGetNamingPolicy(CppNamingPolicySuggestionContext context, IPsiSourceFile? sourceFile, IContextBoundSettingsStore? settingsStore) => null;
    }
}