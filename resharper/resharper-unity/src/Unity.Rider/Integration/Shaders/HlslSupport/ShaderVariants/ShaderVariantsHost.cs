#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.RdBackend.Common.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost : IChangeProvider
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IDocumentHost myDocumentHost;
    private readonly ITextControlHost myTextControlHost;
    private readonly ShaderVariantsManager myShaderVariantsManager;
    private readonly IPreferredRootFileProvider myPreferredPreferredRootFileProvider;
    private readonly ILogger myLogger;
    private readonly Dictionary<TextControlId, ShaderVariantRegistration> myActiveControls = new();
    private readonly Dictionary<TextControlId, LifetimeDefinition> myShaderVariantExtensionLifetimes = new();

    public ShaderVariantsHost(Lifetime lifetime,
        ISolution solution,
        ShaderProgramCache shaderProgramCache,
        IPreferredRootFileProvider preferredRootFileProvider,
        ISettingsStore settingsStore,
        [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _,
        ChangeManager changeManager,
        ILogger logger,
        IDocumentHost documentHost,
        ITextControlHost textControlHost,
        ShaderVariantsManager shaderVariantsManager,
        IPsiModules psiModules,
        ShaderContextCache shaderContextCache,
        FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myPreferredPreferredRootFileProvider = preferredRootFileProvider;
        myDocumentHost = documentHost;
        myTextControlHost = textControlHost;
        myShaderVariantsManager = shaderVariantsManager;
        myLogger = logger;

        frontendBackendHost?.Do(model =>
        {
            model.CreateShaderVariantInteraction.SetAsync(HandleCreateShaderVariantInteraction);
            model.ShaderVariantExtensions.Advise(lifetime, mapEvent => OnShaderVariantExtensionsModified(lifetime, mapEvent));
            myShaderVariantsManager.AllEnabledKeywords.Advise(lifetime, OnEnabledKeywordsChange);
            
            myTextControlHost.ViewHostTextControls(lifetime, OnTextControlAdded);
            
            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, this, shaderVariantsManager);
            changeManager.AddDependency(lifetime, this, psiModules);
            changeManager.AddDependency(lifetime, this, shaderContextCache);
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

        var shaderProgramInfo = GetShaderProgramInfo(textControl.Caret.DocumentOffset());
        var reg = new ShaderVariantRegistration(shaderVariantExtension, textControl, shaderProgramInfo);
        UpdateShaderVariant(reg);
        myActiveControls.Add(extensionLifetime, textControlId, reg);
        textControl.Caret.Position.Change.Advise(textControl.Lifetime, _ => RevalidateShaderVariant(reg));
    }

    private async Task<ShaderVariantInteraction> HandleCreateShaderVariantInteraction(Lifetime lifetime, CreateShaderVariantInteractionArgs args)
    {
        myLogger.Verbose("Start shader variant interaction");
        var interaction = await lifetime.StartBackgroundRead(() =>
        {
            List<RdShaderKeyword>? keywords = null;
            if (myDocumentHost.TryGetDocument(args.DocumentId) is { } document && TryGetRootLocation(new DocumentOffset(document, args.Offset), out var rootLocation) && myShaderProgramCache.TryGetShaderProgramInfo(rootLocation, out var shaderProgramInfo))
            {
                var enabledKeywords = myShaderVariantsManager.GetEnabledKeywords(rootLocation);
                keywords = new List<RdShaderKeyword>();
                foreach (var keyword in shaderProgramInfo.Keywords)
                    keywords.Add(new RdShaderKeyword(keyword, enabledKeywords.Contains(keyword)));
            }
            
            return new ShaderVariantInteraction(keywords ?? new List<RdShaderKeyword>(), myShaderVariantsManager.ShaderApi.AsRdShaderApi(), myShaderVariantsManager.ShaderPlatform.AsRdShaderPlatform(), myShaderVariantsManager.TotalKeywordsCount.Value, myShaderVariantsManager.TotalEnabledKeywordsCount.Value);
        });
        interaction.EnableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, true));
        interaction.DisableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, false));
        interaction.SetShaderApi.Advise(lifetime, api => myShaderVariantsManager.SetShaderApi(api.AsShaderApi()));
        interaction.SetShaderPlatform.Advise(lifetime, platform => myShaderVariantsManager.SetShaderPlatform(platform.AsShaderPlatform()));
        return interaction;
    }

    private bool TryGetRootLocation(DocumentOffset documentOffset, out CppFileLocation rootLocation)
    {
        if (documentOffset.Document is RiderDocument document)
        {
            if (document.Location.Name.EndsWith(ShaderLabProjectFileType.SHADERLAB_EXTENSION))
            {
                if (document.GetPsiSourceFile(mySolution) is { } sourceFile &&
                    sourceFile.GetPrimaryPsiFile()?.FindTokenAt(new TreeOffset(documentOffset.Offset)) is { } token &&
                    token.NodeType == ShaderLabTokenType.CG_CONTENT)
                    rootLocation = new CppFileLocation(sourceFile, TextRange.FromLength(token.GetTreeStartOffset().Offset, token.GetTextLength()));
            }
            else
                rootLocation = myPreferredPreferredRootFileProvider.GetPreferredRootFile(new CppFileLocation(document.Location));
        }

        return rootLocation.IsValid();
    }

    private ShaderProgramInfo? GetShaderProgramInfo(DocumentOffset documentOffset)
    {
        using (ReadLockCookie.Create())
            return TryGetRootLocation(documentOffset, out var rootLocation) && myShaderProgramCache.TryGetShaderProgramInfo(rootLocation, out var shaderProgramInfo) ? shaderProgramInfo : null;
    }

    public object? Execute(IChangeMap changeMap)
    {
        foreach (var reg in myActiveControls.Values)
            RevalidateShaderVariant(reg);

        return null;
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