using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    public interface IUnitySolutionSettingsProvider
    {
        void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint);
    }

    public interface IUnityProjectSettingsProvider
    {
        void InitialiseProjectSettings(Lifetime projectLifetime, IProject project, ISettingsStorageMountPoint mountPoint);
    }
}