#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.RdBackend.Common.Features.TextControls;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
public class ShaderVariantsHost : IShaderVariantsHost, IChangeProvider, IUnityProjectFolderLazyComponent
{
    private readonly ISolution mySolution;
    private readonly CppExternalModule myCppExternalModule;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IDocumentHost myDocumentHost;
    private readonly ITextControlHost myTextControlHost;
    private readonly ShaderVariantsManager myShaderVariantsManager;
    private readonly IPsiModules myPsiModules;
    private readonly DocumentManager myDocumentManager;
    private readonly ShaderContextCache myShaderContextCache;
    private readonly IPreferredRootFileProvider myPreferredRootFileProvider;
    private readonly FrontendBackendHost? myFrontendBackendHost;
    private readonly ILogger myLogger;
    private readonly Dictionary<TextControlId, ShaderVariantRegistration> myActiveControls = new();
    private readonly Dictionary<TextControlId, LifetimeDefinition> myShaderVariantExtensionLifetimes = new();

    public ShaderVariantsHost(Lifetime lifetime,
        ISolution solution,
        ShaderProgramCache shaderProgramCache,
        IPreferredRootFileProvider preferredRootFileProvider,
        ChangeManager changeManager,
        ILogger logger,
        IDocumentHost documentHost,
        ITextControlHost textControlHost,
        ShaderVariantsManager shaderVariantsManager,
        IPsiModules psiModules,
        ShaderContextCache shaderContextCache, CppExternalModule cppExternalModule, DocumentManager documentManager, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myPreferredRootFileProvider = preferredRootFileProvider;
        myDocumentHost = documentHost;
        myTextControlHost = textControlHost;
        myShaderVariantsManager = shaderVariantsManager;
        myPsiModules = psiModules;
        myDocumentManager = documentManager;
        myShaderContextCache = shaderContextCache;
        myCppExternalModule = cppExternalModule;
        myDocumentManager = documentManager;
        myLogger = logger;
        myFrontendBackendHost = frontendBackendHost;

        solution.Locks.ExecuteOrQueueEx(lifetime, "ShaderVariantsHost", () =>
        {
            myFrontendBackendHost?.Do(model =>
            {
                model.CreateShaderVariantInteraction.SetAsync(HandleCreateShaderVariantInteraction);
                model.ShaderVariantExtensions.Advise(lifetime, mapEvent => OnShaderVariantExtensionsModified(lifetime, mapEvent));
                myShaderVariantsManager.AllEnabledKeywords.Advise(lifetime, OnEnabledKeywordsChange);
            
                myTextControlHost.ViewHostTextControls(lifetime, OnTextControlAdded);
            
                changeManager.RegisterChangeProvider(lifetime, this);
                changeManager.AddDependency(lifetime, this, psiModules);         // aware of UnityShadersModule load event 
                changeManager.AddDependency(lifetime, this, shaderContextCache); // aware of shader context changes 

                myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => RevalidateAll());
            });
        });
    }

    private void OnShaderVariantExtensionsModified(Lifetime lifetime, MapEvent<TextControlId, RdShaderVariantExtension> mapEvent)
    {
        // TODO: Use TextControlModel extension when RD fix issue with async serializers initialisation (there now no guarantee that extension's serializer from Unity plugin registered before first text control model creation and causes exception with Polymorphic serializer non-registered)
        switch (mapEvent.Kind)
        {
            case AddUpdateRemove.Update:
                RemoveExtensionMapping(mapEvent.Key);
                AddExtensionMapping(lifetime, mapEvent.Key, mapEvent.NewValue);
                break;
            case AddUpdateRemove.Add:
                AddExtensionMapping(lifetime, mapEvent.Key, mapEvent.NewValue);
                break;
            case AddUpdateRemove.Remove:
                RemoveExtensionMapping(mapEvent.Key);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void OnEnabledKeywordsChange(SetEvent<string> setEvent)
    {
        // State access only allowed from main thread  
        mySolution.Locks.AssertMainThread();

        var keyword = setEvent.Value;
        foreach (var reg in myActiveControls.Values)
        {
            if (reg.ShaderProgramInfo is {} shaderProgramInfo && shaderProgramInfo.HasKeyword(keyword))
                UpdateShaderVariant(reg);
        }
    }

    private void OnTextControlAdded(Lifetime lifetime, TextControlId textControlId, ITextControl textControl)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        if (myActiveControls.TryGetValue(textControlId, out var reg) && reg.TextControl != textControl)
        {
            reg.TextControl = textControl;
            RevalidateShaderVariant(reg);
            textControl.Caret.Position.Change.Advise(textControl.Lifetime, _ => RevalidateShaderVariant(reg));
        }
    }

    private void RemoveExtensionMapping(TextControlId textControlId)
    {
        if (myShaderVariantExtensionLifetimes.Remove(textControlId, out var lifetimeDefinition))
            lifetimeDefinition.Terminate();
    }

    private void AddExtensionMapping(Lifetime lifetime, TextControlId textControlId, RdShaderVariantExtension? extension)
    {
        if (extension == null || myTextControlHost.TryGetTextControl(textControlId) is not {} textControl)
            return;

        var extensionLifetime = lifetime.CreateNested();
        myShaderVariantExtensionLifetimes.Add(textControlId, extensionLifetime);
        OnShaderVariantExtensionAdded(extensionLifetime.Lifetime, textControlId, textControl, extension);
    }

    private void OnShaderVariantExtensionAdded(Lifetime extensionLifetime, TextControlId textControlId, ITextControl textControl, RdShaderVariantExtension shaderVariantExtension)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        var reg = new ShaderVariantRegistration(shaderVariantExtension, textControl, null);
        shaderVariantExtension.Info.Value = new RdShaderVariantInfo(0, 0, 0);
        myActiveControls.Add(extensionLifetime, textControlId, reg);
        
        RevalidateShaderVariant(reg);
        textControl.Caret.Position.Change.Advise(textControl.Lifetime, _ => RevalidateShaderVariant(reg));
    }

    private async Task<ShaderVariantInteraction> HandleCreateShaderVariantInteraction(Lifetime lifetime, CreateShaderVariantInteractionArgs args)
    {
        myLogger.Verbose("Start shader variant interaction");
        var interaction = await lifetime.StartBackgroundRead(() =>
        {
            List<List<string>> shaderFeatures = new();
            var enabledKeywords = myShaderVariantsManager.AllEnabledKeywords.ToList();
            var availableCount = 0;
            if (myDocumentHost.TryGetDocument(args.DocumentId) is { } document && new DocumentOffset(document, args.Offset) is var documentOffset && GetShaderProgramInfo(documentOffset) is {} shaderProgramInfo) 
            {
                foreach (var feature in shaderProgramInfo.ShaderFeatures)
                    shaderFeatures.Add(feature.Entries.Select(e => e.Keyword).ToList());

                availableCount = shaderProgramInfo.Keywords.Count;
            }
            
            return new ShaderVariantInteraction(shaderFeatures, enabledKeywords, myShaderVariantsManager.ShaderApi.AsRdShaderApi(), myShaderVariantsManager.ShaderPlatform.AsRdShaderPlatform(), myShaderVariantsManager.UrtCompilationMode.AsRdUrtCompilationMode(), myShaderVariantsManager.TotalKeywordsCount.Value, myShaderVariantsManager.TotalEnabledKeywordsCount.Value, availableCount);
        });
        interaction.EnableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, true));
        interaction.DisableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, false));
        interaction.DisableKeywords.Advise(lifetime, keywords => myShaderVariantsManager.SetKeywordsEnabled(keywords, false));
        interaction.SetShaderApi.Advise(lifetime, api => myShaderVariantsManager.SetShaderApi(api.AsShaderApi()));
        interaction.SetShaderPlatform.Advise(lifetime, platform => myShaderVariantsManager.SetShaderPlatform(platform.AsShaderPlatform()));
        interaction.SetUrtCompilationMode.Advise(lifetime, urtMode => myShaderVariantsManager.SetUrtCompilationMode(urtMode.AsUrtCompilationMode()));
        return interaction;
    }

    private ShaderProgramInfo? GetShaderProgramInfo(DocumentOffset documentOffset)
    {
        if (documentOffset.Document is not RiderDocument document) return null;
        
        ShaderProgramInfo? shaderProgramInfo;
        using var cookie = ReadLockCookie.Create();
        if (document.Location.Name.EndsWith(ShaderLabProjectFileType.SHADERLAB_EXTENSION))
            myShaderProgramCache.TryGetShaderProgramInfo(document.Location, documentOffset.Offset, out shaderProgramInfo);
        else if (myPreferredRootFileProvider.GetPreferredRootFile(new CppFileLocation(document.Location)) is var preferredRoot && preferredRoot.IsValid())
            myShaderProgramCache.TryGetShaderProgramInfo(preferredRoot, out shaderProgramInfo);
        else
            shaderProgramInfo = null;
        return shaderProgramInfo;
    }

    public void ShowShaderVariantInteraction(DocumentOffset offset, ShaderVariantInteractionOrigin origin, IEnumerable<string>? scopeKeywords)
    {
        myFrontendBackendHost?.Do(model =>
        {
            if (myDocumentManager.TryGetProjectFile(offset.Document)?.GetDocumentIdAndModel() is { documentId: {} documentId })
                model.ShowShaderVariantInteraction(new ShowShaderVariantInteractionArgs(documentId, offset.Offset, origin, scopeKeywords?.ToList()));
        });
    }

    public object? Execute(IChangeMap changeMap)
    {
        if (changeMap.GetChange<PsiModuleChange>(myPsiModules) is { } moduleChange && moduleChange.ModuleChanges.Any(x => x.Item is UnityShaderModule)) 
            RevalidateAll();
        else if (changeMap.GetChange<CppChange>(myShaderContextCache) is { } cppChange)
        {
            foreach (var changedFile in cppChange.ChangedFiles)
            {
                if (changedFile.GetDocument(mySolution, myCppExternalModule) is not {} document) continue;
                foreach (var textControlId in myTextControlHost.GetTextControlIds(document))
                {
                    if (myActiveControls.TryGetValue(textControlId, out var reg))
                        RevalidateShaderVariant(reg);
                }
            }
        }

        return null;
    }

    private void RevalidateAll()
    {
        foreach (var reg in myActiveControls.Values)
            RevalidateShaderVariant(reg);
    }

    private void RevalidateShaderVariant(ShaderVariantRegistration shaderVariantRegistration)
    {
        var shaderProgramInfo = GetShaderProgramInfo(shaderVariantRegistration.TextControl.Caret.DocumentOffset());
        if (shaderProgramInfo != shaderVariantRegistration.ShaderProgramInfo)
        {
            shaderVariantRegistration.ShaderProgramInfo = shaderProgramInfo;
            UpdateShaderVariant(shaderVariantRegistration);
        }
    }
    
    private void UpdateShaderVariant(ShaderVariantRegistration shaderVariantRegistration)
    {
        var enabledKeywords = myShaderVariantsManager.AllEnabledKeywords;
        var shaderProgramInfo = shaderVariantRegistration.ShaderProgramInfo;
        
        var enabled = 0;
        var suppressed = 0;
        var available = shaderProgramInfo?.Keywords.Count ?? 0;
        if (available > 0)
        {
            var trulyEnabled = new HashSet<string>();
            foreach (var feature in shaderProgramInfo!.ShaderFeatures)
            {
                foreach (var entry in feature.Entries)
                {
                    if (!enabledKeywords.Contains(entry.Keyword)) continue;
                    trulyEnabled.Add(entry.Keyword);
                    break;
                }
            }

            foreach (var keyword in shaderProgramInfo.Keywords)
            {
                if (trulyEnabled.Contains(keyword))
                    ++enabled;
                else if (enabledKeywords.Contains(keyword))
                    ++suppressed;
            }
        }

        shaderVariantRegistration.ShaderVariant.Info.Value = new RdShaderVariantInfo(enabled, suppressed, available);
    }

    private class ShaderVariantRegistration
    {
        public ShaderVariantRegistration(RdShaderVariantExtension shaderVariant, ITextControl textControl, ShaderProgramInfo? shaderProgramInfo)
        {
            ShaderVariant = shaderVariant;
            TextControl = textControl;
            ShaderProgramInfo = shaderProgramInfo;
        }

        public RdShaderVariantExtension ShaderVariant { get; }
        public ITextControl TextControl { get; set; }
        public ShaderProgramInfo? ShaderProgramInfo { get; set; }
    }
}