using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IUnitySolutionSettingsProvider
    {
        void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint);
    }

    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IUnityProjectSettingsProvider
    {
        void InitialiseProjectSettings(Lifetime projectLifetime, IProject project, ISettingsStorageMountPoint mountPoint);
    }
}