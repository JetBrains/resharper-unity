using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers.PropertyModifiers;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi
{
    [SolutionFeaturePart(Instantiation.DemandAnyThreadUnsafe)]
    internal sealed class UnityCSharpLanguageLevelProvider([NotNull] CSharpLanguageLevelProjectProperty projectProperty)
        : CSharpLanguageLevelProvider(projectProperty)
    {
        private static readonly Key<CachedProjectLanguageLevel> ourGeneratedProjectLanguageLevelKey = new(nameof(ourGeneratedProjectLanguageLevelKey));

        public override ILanguageVersionModifier<CSharpLanguageVersion> LanguageVersionModifier => null; // disable ability to modify language version

        public override bool IsApplicable(IPsiModule psiModule)
        {
            // applicable only to generated C# projects (class libraries can use whatever language version they want)
            return psiModule is IProjectPsiModule projectPsiModule
#pragma warning disable CS0618 // Type or member is obsolete
                   && projectPsiModule.PsiLanguage.Is<CSharpLanguage>()
#pragma warning restore CS0618 // Type or member is obsolete
                   && psiModule.ContainingProjectModule is IProject project
                   && project.IsUnityGeneratedProject();
        }

        public override CSharpLanguageLevel GetLatestAvailableLanguageLevel(IPsiModule psiModule )=> GetCachedProjectLanguageLevel(psiModule).GetValue().previewAvailableLanguageValue ?? base.GetLatestAvailableLanguageLevel(psiModule);

        public override CSharpLanguageLevel GetLanguageLevel(IPsiModule psiModule) => GetCachedProjectLanguageLevel(psiModule).GetValue().languageLevel ?? base.GetLanguageLevel(psiModule);

        private static CachedProjectLanguageLevel GetCachedProjectLanguageLevel(IPsiModule psiModule)
        {
            var project = (IProject)psiModule.ContainingProjectModule.NotNull();

            var cachedProjectLanguageLevel = project.GetOrCreateDataNoLock(
                ourGeneratedProjectLanguageLevelKey,
                project,
                static project => new CachedProjectLanguageLevel(project));
            return cachedProjectLanguageLevel;
        }

        public override bool IsAvailable(CSharpLanguageLevel languageLevel, IPsiModule psiModule)
        {
            var latestAvailableLanguageLevel = GetLatestAvailableLanguageLevel(psiModule);
            return languageLevel <= latestAvailableLanguageLevel;
        }

        public override bool IsAvailable(CSharpLanguageVersion languageVersion, IPsiModule psiModule)
        {
            var languageLevel = ConvertToLanguageLevel(languageVersion, psiModule);
            var latestAvailableLanguageLevel = GetLatestAvailableLanguageLevel(psiModule);

            return languageLevel <= latestAvailableLanguageLevel;
        }

        public override CSharpLanguageLevel ConvertToLanguageLevel(CSharpLanguageVersion languageVersion, IPsiModule psiModule)
        {
            if (languageVersion is CSharpLanguageVersion.Default or CSharpLanguageVersion.Latest or CSharpLanguageVersion.LatestMajor)
                return GetCachedProjectLanguageLevel(psiModule).GetValue().latestAvailableLanguageValue ?? base.ConvertToLanguageLevel(languageVersion, psiModule);

            if (languageVersion is CSharpLanguageVersion.Preview)
                return GetCachedProjectLanguageLevel(psiModule).GetValue().previewAvailableLanguageValue ?? base.ConvertToLanguageLevel(languageVersion, psiModule);

            return base.ConvertToLanguageLevel(languageVersion, psiModule);
        }

        private sealed class CachedProjectLanguageLevel([NotNull] IProject project)
            : CachedProjectItemAnyChange<IProject, (CSharpLanguageLevel? languageLevel, CSharpLanguageLevel? latestAvailableLanguageValue, CSharpLanguageLevel? previewAvailableLanguageValue)>(project.GetSolution().Timestamps, project, Evaluate)
        {
            private static (CSharpLanguageLevel? languageLevel, CSharpLanguageLevel? latestAvailableLanguageValue, CSharpLanguageLevel? previewAvailableLanguageValue) Evaluate(IProject project)
            {
                #region Explanation
                // In Unity 2019.2 - Unity 6
                // LangVersion is specified, can be modified by csc.rsp
                // There is a certain amount of C# lang features which don't respect LangVersion, but depend on Roslyn capabilities,
                // thus we need latestAvailableLanguageValue.
                // see: RIDER-119992 Incorrect not initialized variable errors
                //
                // Older Unity versions:
                // Make sure we don't suggest code changes that won't compile in Unity due to mismatched C# language levels
                // (e.g. C#6 "elvis" operator)
                //
                // * Unity prior to 5.5 uses an old mono compiler that only supports C# 4
                // * Unity 5.5 and later adds C# 6 support as an option. This is enabled by setting
                //   the API compatibility level to NET_4_6
                // * The CSharp60Support plugin replaces the compiler with either C# 6 or C# 7.0 or 8.0
                //   It can be recognised by a folder called `CSharp60Support` or `CSharp70Support` or `CSharp80Support`
                //   in the root of the project
                //   (https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration)
                // * Note that since Unity 2017.2 till 2018.3, we've been special-cased in the Unity csproj generation
                //   and we've been getting v4.5 for old runtime and default values (4.7.1) for new. So where
                //   it says 3.5 below, that depends on the version of Unity. Older versions will give us 3.5,
                //   newer versions 4.5.
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
                // * Unity 2018.x+. LangVersion is `latest`. MSBuild 16 treats `newest` as C# 8. Re# and Rider start suggesting it.
                //
                // Actions:
                // * If `LangVersion` is missing or "default"
                // then override based on `TargetFrameworkVersion` or presence of `CSharp60Support`/`CSharp70Support`
                // else do nothing
                // * If `LangVersion` is "latest"
                // then override based on `CSharp80Support` presence or LangVersion matching Roslyn bundled in Unity
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

                var unityProjectFileCacheProvider = project.GetComponent<UnityProjectFileCacheProvider>();
                var appPath = unityProjectFileCacheProvider.GetAppPath(project);
                var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
                if (!contentPath.IsNullOrEmpty())
                {
                    var roslynDir = contentPath.Combine("Tools").Combine("Roslyn"); // older location
                    if (!roslynDir.ExistsDirectory)
                        roslynDir = contentPath.Combine("DotNetSdkRoslyn"); // Unity 6 location and maybe others

                    if (roslynDir.ExistsDirectory)
                    {
                        var languageLevelProjectProperty = project.GetComponent<CSharpLanguageLevelProjectProperty>();
                        var langVersion = project.ProjectProperties.ActiveConfigurations.Configurations
                            .OfType<CSharpProjectConfiguration>()
                            .Select(configuration => configuration.LanguageVersion)
                            .FirstOrDefault(CSharpLanguageVersion.Default);
                        var languageLevel = languageLevelProjectProperty.ConvertToLanguageLevel(langVersion, roslynDir);
                        return (languageLevel, languageLevelProjectProperty.ConvertToLanguageLevel(CSharpLanguageVersion.Latest, roslynDir), languageLevelProjectProperty.ConvertToLanguageLevel(CSharpLanguageVersion.Preview, roslynDir));
                    }
                }
                
                return DetermineCSharpLanguageLevelOldUnity(project, unityProjectFileCacheProvider);
            }

            private static readonly Version ourVersion46 = new(4, 6);
            private static (CSharpLanguageLevel? languageLevel, CSharpLanguageLevel? latestAvailableLanguageValue,CSharpLanguageLevel? previewAvailableLanguageValue)
                DetermineCSharpLanguageLevelOldUnity(IProject project, UnityProjectFileCacheProvider unityProjectFileCacheProvider)
            {
                
                CSharpLanguageLevel? languageLevel = null;
                if (!unityProjectFileCacheProvider.IsLangVersionExplicitlySpecified(project) || IsLangVersionDefault())
                {
                    // Support for https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration
                    // See also https://github.com/JetBrains/resharper-unity/issues/50#issuecomment-257611218
                    if (project.Location.CombineWithShortName("CSharp70Support").ExistsDirectory)
                        languageLevel = CSharpLanguageLevel.CSharp70;
                    else if (project.Location.CombineWithShortName("CSharp60Support").ExistsDirectory)
                        languageLevel = CSharpLanguageLevel.CSharp60;
                    else 
                        languageLevel = IsTargetFrameworkAtLeast46()
                            ? CSharpLanguageLevel.CSharp60
                            : CSharpLanguageLevel.CSharp40;
                }
                
                // https://forum.unity.com/threads/would-the-roslyn-compiler-compile-c-8-0-preview.598069/
                if (project.Location.CombineWithShortName("CSharp80Support").ExistsDirectory)
                {
                    if (IsLangVersionLatest()) languageLevel ??= CSharpLanguageLevel.CSharp80;
                }

                bool IsLangVersionDefault()
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

                bool IsLangVersionLatest()
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

                bool IsTargetFrameworkAtLeast46()
                {
                    return project.GetCurrentTargetFrameworkId().Version >= ourVersion46;
                }

                return (languageLevel, languageLevel, languageLevel);
            }
        }
    }
}