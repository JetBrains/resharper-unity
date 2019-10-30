using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class LangVersionSetting : IUnityProjectSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ILanguageLevelProjectProperty<CSharpLanguageLevel, CSharpLanguageVersion> myLanguageLevelProjectProperty;
        private static readonly Version ourVersion46 = new Version(4, 6);

        public LangVersionSetting(ISettingsSchema settingsSchema, ILogger logger,
                                  UnityProjectFileCacheProvider unityProjectFileCache,
                                  ILanguageLevelProjectProperty<CSharpLanguageLevel, CSharpLanguageVersion> languageLevelProjectProperty)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
            myUnityProjectFileCache = unityProjectFileCache;
            myLanguageLevelProjectProperty = languageLevelProjectProperty;
        }

        public void InitialiseProjectSettings(Lifetime projectLifetime, IProject project,
                                              ISettingsStorageMountPoint mountPoint)
        {
            SetProjectLangVersion(project, mountPoint);

            // If the project data cache isn't ready yet, or changes at a later date, reset the overridden lang version
            myUnityProjectFileCache.RegisterDataChangedCallback(projectLifetime, project.ProjectFileLocation,
                () => SetProjectLangVersion(project, mountPoint));
        }

        private void SetProjectLangVersion(IProject project, ISettingsStorageMountPoint mountPoint)
        {
            // Only if it's a generated project. Class libraries can use whatever language version they want
            if (!project.IsUnityGeneratedProject())
                return;

            #region Explanation
            // Make sure we don't suggest code changes that won't compile in Unity due to mismatched C# language levels
            // (e.g. C#6 "elvis" operator)
            //
            // * Unity prior to 5.5 uses an old mono compiler that only supports C# 4
            // * Unity 5.5 and later adds C# 6 support as an option. This is enabled by setting
            //   the API compatibility level to NET_4_6
            // * The CSharp60Support plugin replaces the compiler with either C# 6 or C# 7.0
            //   It can be recognised by a folder called `CSharp60Support` or `CSharp70Support`
            //   in the root of the project
            //   (https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration)
            //
            // Scenarios:
            // * No VSTU installed (including Unity 5.5)
            //   .csproj has NO `LangVersion`. `TargetFrameworkVersion` will be `v3.5`
            // * Early versions of VSTU
            //   .csproj has NO `LangVersion`. `TargetFrameworkVersion` will be `v3.5`
            // * Later versions of VSTU
            //   `LangVersion` is correctly set to "4". `TargetFrameworkVersion` will be `v3.5`
            //   OR `LangVersion` is set to "6" or "latest".
            // * VSTU for 5.5
            //   `LangVersion` is set to "default". `TargetFrameworkVersion` will be `v3.5` or `v4.6`
            //   Note that "default" for VS"15" or Rider will be C# 7.0!
            // * Unity3dRider is installed
            //   Uses Unity's own generation and adds correct `LangVersion`
            //   `TargetFrameworkVersion` will be correct for the selected runtime
            // * CSharp60Support is installed
            //   .csproj has NO `LangVersion`
            //   `TargetFrameworkVersion` is NOT accurate (support for C# 6 is not dependent on/trigger by .net 4.6)
            //   Look for `CSharp60Support` or `CSharp70Support` folders
            //
            // Actions:
            // * If `LangVersion` is missing or "default"
            // then override based on `TargetFrameworkVersion` or presence of `CSharp60Support`/`CSharp70Support`
            // else do nothing
            //
            // Notes:
            // * Unity and VSTU have two separate .csproj routines. VSTU adds extra references,
            //   the VSTU project flavour GUID and imports UnityVs.targets, which disables the
            //   `GenerateTargetFrameworkMonikerAttribute` target
            // * CSharp60Support post-processes the .csproj file directly if VSTU is not installed.
            //   If it is installed, it registers a delegate with `ProjectFilesGenerator.ProjectFileGeneration`
            //   and removes it before it's written to disk
            // * `LangVersion` can be conditionally specified, which makes checking for "default" awkward
            // * If Unity3dRider + CSharp60Support are both installed, last write wins
            //   Order of post-processing is non-deterministic, so Rider's LangVersion might be removed
            #endregion

            #region Explanation "Avoid C# 8"
            // Unity 2018.x+ set LangVersion to `newest`
            // MSBuild 16 treats `newest` as C# 8. Re# and Rider start suggesting it.
            
            #endregion

            var languageLevel = ReSharperSettingsCSharpLanguageLevel.Default;
            if (IsLangVersionMissing(project) || IsLangVersionDefault(project))
            {
                // Support for https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration
                // See also https://github.com/JetBrains/resharper-unity/issues/50#issuecomment-257611218
                if (project.Location.CombineWithShortName("CSharp70Support").ExistsDirectory)
                    languageLevel = ReSharperSettingsCSharpLanguageLevel.CSharp70;
                else if (project.Location.CombineWithShortName("CSharp60Support").ExistsDirectory)
                    languageLevel = ReSharperSettingsCSharpLanguageLevel.CSharp60;
                else
                {
                    languageLevel = IsTargetFrameworkAtLeast46(project)
                        ? ReSharperSettingsCSharpLanguageLevel.CSharp60
                        : ReSharperSettingsCSharpLanguageLevel.CSharp40;
                }
            }

            if (IsLangVersionLatest(project))
            {
                var appPath = myUnityProjectFileCache.GetAppPath(project);
                var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
                var dllPath = contentPath.Combine(@"Tools\Roslyn\Microsoft.CodeAnalysis.dll");

                if (dllPath.ExistsFile)
                    languageLevel = myLanguageLevelProjectProperty.GetLatestAvailableLanguageLevel(dllPath.Directory).ToSettingsLanguageLevel();
            }

            // Always set a value. It's either the overridden value, or Default, which resets to whatever is in the
            // project file
            SetValue(mountPoint, (CSharpLanguageProjectSettings s) => s.LanguageLevel, languageLevel);
        }

        private bool IsLangVersionMissing(IProject project)
        {
            return !myUnityProjectFileCache.IsLangVersionExplicitlySpecified(project);
        }

        private bool IsLangVersionDefault(IProject project)
        {
            // Older VSTU sets LangVersion to "default", which means a higher version than the expected C# 4 or 6
            foreach (var configuration in project.ProjectProperties.ActiveConfigurations.Configurations)
            {
                if (configuration is ICSharpProjectConfiguration csharpConfiguration)
                {
                    // CSharpLanguageVersion.LatestMajor is the enum value for "default"
                    // CSharpLanguageVersion.Latest is the enum value for "latest"
                    if (csharpConfiguration.LanguageVersion != CSharpLanguageVersion.LatestMajor)
                        return false;
                }
            }
            return true;
        }
        
        private bool IsLangVersionLatest(IProject project)
        {
            foreach (var configuration in project.ProjectProperties.ActiveConfigurations.Configurations)
            {
                if (configuration is ICSharpProjectConfiguration csharpConfiguration)
                {
                    if (csharpConfiguration.LanguageVersion == CSharpLanguageVersion.Latest)
                        return true;
                }
            }
            return false;
        }

        private bool IsTargetFrameworkAtLeast46(IProject project)
        {
            return project.GetCurrentTargetFrameworkId().Version >= ourVersion46;
        }

        private void SetValue<TKeyClass, TEntryValue>([NotNull] ISettingsStorageMountPoint mount,
                                                      [NotNull] Expression<Func<TKeyClass, TEntryValue>> entryExpression,
                                                      [NotNull] TEntryValue value,
                                                      IDictionary<SettingsKey, object> keyIndices = null)
        {
            ScalarSettingsStoreAccess.SetValue(mount, mySettingsSchema.GetScalarEntry(entryExpression), keyIndices,
                value, false, null, myLogger);
        }
    }
}