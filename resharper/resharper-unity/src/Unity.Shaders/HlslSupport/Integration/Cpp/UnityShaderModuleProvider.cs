using System;
using System.Collections.Generic;
using JetBrains.DocumentManagers;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Cpp
{
    public class ShaderFilesProperties : IPsiSourceFileProperties
    {
        public ShaderFilesProperties(bool isICacheParticipant)
        {
            IsICacheParticipant = isICacheParticipant;
        }
        public bool ShouldBuildPsi => true;

        public bool IsGeneratedFile => false;

        public bool IsICacheParticipant { get; }

        public bool ProvidesCodeModel => true;

        public bool IsNonUserFile => false;

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

        public UnityShaderModule(ISolution solution, String name, TargetFrameworkId targetFrameworkId)
        {
            mySolution = solution;
            myPersistentId = "UnityShaderModule:" + name;
            TargetFrameworkId = targetFrameworkId;
            Files = new Dictionary<IProjectFile, PsiProjectFile>();
        }

        public string Name => "UnityShaderModule";

        public string DisplayName => "Unity Editor Shader module";

        public TargetFrameworkId TargetFrameworkId { get; }

        public ISolution GetSolution()
        {
            return mySolution;
        }


        public Dictionary<IProjectFile, PsiProjectFile> Files { get; }

        public IEnumerable<IPsiSourceFile> GetPsiSourceFileFor(IProjectFile projectFile)
        {
            if (Files.ContainsKey(projectFile))
                return new[] {Files[projectFile]};
            return EmptyList<IPsiSourceFile>.InstanceList;
        }

        public PsiLanguageType PsiLanguage => CppLanguage.Instance;

        public ProjectFileType ProjectFileType => KnownProjectFileType.Instance;

        public IEnumerable<IPsiModuleReference> GetReferences(
            IModuleReferenceResolveContext moduleReferenceResolveContext)
        {
            return EmptyList<IPsiModuleReference>.InstanceList;
        }

        public IModule ContainingProjectModule => null;

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


    [SolutionComponent]
    public class UnityShaderPsiModuleProviderFilter : IProjectPsiModuleProviderFilter
    {
        private readonly UnityReferencesTracker myTracker;

        public UnityShaderPsiModuleProviderFilter(UnityReferencesTracker tracker)
        {
            myTracker = tracker;
        }

        public Tuple<IProjectPsiModuleHandler, IPsiModuleDecorator> OverrideHandler(Lifetime lifetime, IProject project,
            IProjectPsiModuleHandler handler)
        {
            if ( handler.PrimaryModule != null && myTracker.IsUnityProject(project))
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
        private UnityShaderModule myModule;
        private ShaderFilesProperties myShaderLabProperties;
        private ShaderFilesProperties myCppProperties;

        public UnityShaderModuleHandlerAndDecorator(
            UnityShaderModule module,
            IProjectPsiModuleHandler handler)
            : base(handler)
        {
            myAllModules = new List<IPsiModule>(base.GetAllModules());
            myAllModules.Add(module);

            myShaderLabProperties = new ShaderFilesProperties(true);
            myCppProperties = new ShaderFilesProperties(false);
            myModule = module;
            myDocumentManager = module.GetSolution().GetComponent<DocumentManager>();
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
                    (file, sf) => sf.GetLocation().ExtensionWithDot.Equals(ShaderLabProjectFileType.SHADERLAB_EXTENSION) ? myShaderLabProperties : myCppProperties,
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
    }
}