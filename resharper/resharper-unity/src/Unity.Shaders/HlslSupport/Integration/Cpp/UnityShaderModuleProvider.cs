#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentManagers;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;

public class ShaderFilesProperties(bool isICacheParticipant) : IPsiSourceFileProperties
{
    internal static readonly ShaderFilesProperties ShaderLabUserFileProperties = new(true);
    internal static readonly ShaderFilesProperties ShaderLabPackageLocalCacheFileProperties = new(true) { IsNonUserFile = true };
    internal static readonly ShaderFilesProperties ComputeShaderFilesProperties = new(true);
    internal static readonly ShaderFilesProperties ComputeShaderPackageLocalCacheFilesProperties = new(true) { IsNonUserFile = true };
    internal static readonly ShaderFilesProperties NoCacheFilesProperties = new(false);
    internal static readonly ShaderFilesProperties HlslUserFileProperties = new(false);
    internal static readonly ShaderFilesProperties HlslPackageLocalCacheFileProperties = new(false) { IsNonUserFile = true };

    public bool ShouldBuildPsi => true;

    public bool IsGeneratedFile => false;

    public bool IsICacheParticipant { get; } = isICacheParticipant;

    public bool ProvidesCodeModel => true;

    public bool IsNonUserFile { get; init; }

    public IEnumerable<string> GetPreImportedNamespaces()
    {
        return EmptyList<string>.Instance;
    }

    public string GetDefaultNamespace() => "";

    public ICollection<PreProcessingDirective> GetDefines()
    {
        return EmptyList<PreProcessingDirective>.Instance;
    }
}

public class UnityShaderModule : UserDataHolder, IPsiModule
{
    private readonly ISolution mySolution;
    private readonly string myPersistentId;

    public UnityShaderModule(ISolution solution, string name, TargetFrameworkId targetFrameworkId)
    {
        mySolution = solution;
        myPersistentId = "UnityShaderModule:" + name;
        TargetFrameworkId = targetFrameworkId;
        Files = new Dictionary<IProjectFile, PsiProjectFile>();
    }

    public string Name => "UnityShaderModule";

    public string DisplayName => "Unity Editor Shader module";

    public TargetFrameworkId TargetFrameworkId { get; }

    public ISolution GetSolution() => mySolution;

    public Dictionary<IProjectFile, PsiProjectFile> Files { get; }

    public IEnumerable<IPsiSourceFile> GetPsiSourceFileFor(IProjectFile projectFile)
    {
        if (Files.TryGetValue(projectFile, out var file))
            return new[] { file };
        return EmptyList<IPsiSourceFile>.InstanceList;
    }

    public PsiLanguageType? PsiLanguage => CppLanguage.Instance;

    public ProjectFileType? ProjectFileType => KnownProjectFileType.Instance;

    public IEnumerable<IPsiModuleReference> GetReferences(
        IModuleReferenceResolveContext? moduleReferenceResolveContext)
    {
        return EmptyList<IPsiModuleReference>.InstanceList;
    }

    public IModule? ContainingProjectModule => null;

    public IEnumerable<IPsiSourceFile> SourceFiles => Files.Values;

    public IPsiServices GetPsiServices()
    {
        return mySolution.GetPsiServices();
    }

    public ICollection<PreProcessingDirective> GetAllDefines()
    {
        return EmptyList<PreProcessingDirective>.InstanceList;
    }

    public bool IsValid()
    {
        return true;
    }

    public string GetPersistentID()
    {
        return myPersistentId;
    }
}


[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityShaderPsiModuleProviderFilter : IProjectPsiModuleProviderFilter
{
    public Tuple<IProjectPsiModuleHandler, IPsiModuleDecorator>? OverrideHandler(Lifetime lifetime, IProject project,
        IProjectPsiModuleHandler handler)
    {
        if (handler.PrimaryModule != null && (project.IsUnityProject() || project.GetComponent<UnitySolutionTracker>().IsUnityProject.HasTrueValue()) && !project.IsPlayerProject())
        {
            var module = new UnityShaderModule(project.GetSolution(), project.Name, handler.PrimaryModule.TargetFrameworkId);
            var newHandlerAndDecorator = new UnityShaderModuleHandlerAndDecorator(module, handler);
            return new Tuple<IProjectPsiModuleHandler, IPsiModuleDecorator>(newHandlerAndDecorator,
                newHandlerAndDecorator);
        }

        return null;
    }
}

public class UnityShaderModuleHandlerAndDecorator : DelegatingProjectPsiModuleHandler, IPsiModuleDecorator
{
    private readonly IList<IPsiModule> myAllModules;
    private readonly DocumentManager myDocumentManager;
    private readonly UnityShaderModule myModule;
    private readonly PackageManager myPackageManager;

    public UnityShaderModuleHandlerAndDecorator(
        UnityShaderModule module,
        IProjectPsiModuleHandler handler)
        : base(handler)
    {
        myAllModules = new List<IPsiModule>(base.GetAllModules());
        myAllModules.Add(module);

        myModule = module;
        var solution = module.GetSolution();
        myPackageManager = solution.GetComponent<PackageManager>();
        myDocumentManager = solution.GetComponent<DocumentManager>();
    }

    public IEnumerable<IPsiModuleReference> OverrideModuleReferences(IEnumerable<IPsiModuleReference> references)
    {
        return references;
    }

    public override IList<IPsiModule> GetAllModules()
    {
        return myAllModules;
    }

    public IEnumerable<IPsiSourceFile> OverrideSourceFiles(IEnumerable<IPsiSourceFile> files)
    {
        return files;
    }

    public override IEnumerable<IPsiSourceFile> GetPsiSourceFilesFor(IProjectFile projectFile)
    {
        var extension = projectFile.Location.ExtensionWithDot;
        if (!CppProjectFileType.ALL_HLSL_EXTENSIONS.Contains(extension) &&
            !ShaderLabProjectFileType.SHADERLAB_EXTENSION.Equals(extension))
            return base.GetPsiSourceFilesFor(projectFile);

        if (myModule.Files.TryGetValue(projectFile, out var psiFile))
            return FixedList.Of(psiFile);

        return EmptyList<IPsiSourceFile>.Instance;
    }

    public override void OnProjectFileChanged(IProjectFile projectFile, VirtualFileSystemPath oldLocation,
        PsiModuleChange.ChangeType changeType,
        PsiModuleChangeBuilder changeBuilder)
    {
        var extension = VirtualFileSystemPath.TryParse(projectFile.Name, InteractionContext.SolutionContext).ExtensionWithDot;
        if (!CppProjectFileType.ALL_HLSL_EXTENSIONS.Contains(extension) &&
            !ShaderLabProjectFileType.SHADERLAB_EXTENSION.Equals(extension))
        {
            base.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder);
            return;
        }

        if (changeType == PsiModuleChange.ChangeType.Removed)
        {
            if (myModule.Files.TryGetValue(projectFile, out var psiFile))
            {
                myModule.Files.Remove(projectFile);
                changeBuilder.AddFileChange(psiFile, PsiModuleChange.ChangeType.Removed);
            }
        }
        else if (changeType == PsiModuleChange.ChangeType.Added)
        {
            var sourceFile = new PsiProjectFile(myModule,
                projectFile,
                (file, sf) => GetFileProperties(sf),
                (file, sf) => myModule.Files.ContainsKey(file),
                myDocumentManager,
                BaseHandler.PrimaryModule.GetResolveContextEx(projectFile));

            myModule.Files.Add(projectFile, sourceFile);
            changeBuilder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Added);
        }
        else if (changeType == PsiModuleChange.ChangeType.Modified)
        {
            if (myModule.Files.TryGetValue(projectFile, out var psiFile))
                changeBuilder.AddFileChange(psiFile, PsiModuleChange.ChangeType.Modified);
        }
    }

    private ShaderFilesProperties GetFileProperties(IPsiSourceFile sourceFile)
    {
        var isPackageCacheFile = myPackageManager.IsPackageCacheFile(sourceFile.GetLocation());
        var location = sourceFile.GetLocation();
        if (UnityShaderFileUtils.IsShaderLabFile(location))
            return isPackageCacheFile ? ShaderFilesProperties.ShaderLabPackageLocalCacheFileProperties : ShaderFilesProperties.ShaderLabUserFileProperties;
        if (UnityShaderFileUtils.IsComputeShaderFile(location))
            return isPackageCacheFile ? ShaderFilesProperties.ComputeShaderPackageLocalCacheFilesProperties : ShaderFilesProperties.ComputeShaderFilesProperties;
        return isPackageCacheFile ? ShaderFilesProperties.HlslPackageLocalCacheFileProperties : ShaderFilesProperties.HlslUserFileProperties;
    }
}