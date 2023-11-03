#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IDocumentHost myDocumentHost;
    private readonly ShaderVariantsManager myShaderVariantsManager;
    private readonly IPreferredRootFileProvider myPreferredPreferredRootFileProvider;
    private readonly ILogger myLogger;

    public ShaderVariantsHost(Lifetime lifetime,
        ISolution solution,
        ShaderProgramCache shaderProgramCache,
        IPreferredRootFileProvider preferredRootFileProvider,
        ISettingsStore settingsStore,
        [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _,
        ChangeManager changeManager,
        ILogger logger,
        IDocumentHost documentHost,
        ShaderVariantsManager shaderVariantsManager,
        FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myPreferredPreferredRootFileProvider = preferredRootFileProvider;
        myDocumentHost = documentHost;
        myShaderVariantsManager = shaderVariantsManager;
        myLogger = logger;
        
        frontendBackendHost?.Do(model =>
        {
            model.CreateShaderVariantInteraction.SetAsync(HandleCreateShaderVariantInteraction);
        });
    }

    private async Task<ShaderVariantInteraction> HandleCreateShaderVariantInteraction(Lifetime lifetime, CreateShaderVariantInteractionArgs args)
    {
        myLogger.Verbose("Start shader variant interaction");
        var interaction = await lifetime.StartBackgroundRead(() =>
        {
            var keywords = GetAvailableKeywords(args.DocumentId, args.Offset);
            return new ShaderVariantInteraction(keywords, myShaderVariantsManager.ShaderApi.AsRdShaderApi(), myShaderVariantsManager.TotalKeywordsCount, myShaderVariantsManager.TotalEnabledKeywordsCount);
        });
        interaction.EnableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, true));
        interaction.DisableKeyword.Advise(lifetime, keyword => myShaderVariantsManager.SetKeywordEnabled(keyword, false));
        interaction.SetShaderApi.Advise(lifetime, api => myShaderVariantsManager.SetShaderApi(api.AsShaderApi()));
        return interaction;
    }

    private List<RdShaderKeyword> GetAvailableKeywords(RdDocumentId documentId, int offset)
    {
        var document = myDocumentHost.TryGetDocument(documentId);
        if (document == null)
            return new List<RdShaderKeyword>();
                    
        CppFileLocation rootLocation;
        if (document.Location.Name.EndsWith(ShaderLabProjectFileType.SHADERLAB_EXTENSION))
        {
            if (document.GetPsiSourceFile(mySolution) is { } sourceFile &&
                sourceFile.GetPrimaryPsiFile()?.FindTokenAt(new TreeOffset(offset)) is { } token &&
                token.NodeType == ShaderLabTokenType.CG_CONTENT)
                rootLocation = new CppFileLocation(sourceFile, TextRange.FromLength(token.GetTreeStartOffset().Offset, token.GetTextLength()));
        }
        else
            rootLocation = myPreferredPreferredRootFileProvider.GetPreferredRootFile(new CppFileLocation(document.Location));

        if (!rootLocation.IsValid() || !myShaderProgramCache.TryGetShaderProgramInfo(rootLocation, out var shaderProgramInfo))
            return new List<RdShaderKeyword>();
        
        var enabledKeywords = myShaderVariantsManager.GetEnabledKeywords(rootLocation);
        var availableKeywords = new List<RdShaderKeyword>();
        foreach (var keyword in shaderProgramInfo.Keywords) 
            availableKeywords.Add(new RdShaderKeyword(keyword, enabledKeywords.Contains(keyword)));
        return availableKeywords;
    }
}