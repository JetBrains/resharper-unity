using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.References.Members;

[ReferenceProviderFactory]
public class OdinMemberReferenceFactoryProvider : IReferenceProviderFactory
{
    private readonly UnityTechnologyDescriptionCollector myCollector;

    public OdinMemberReferenceFactoryProvider(UnityTechnologyDescriptionCollector collector)
    {
        myCollector = collector;
        Changed = new Signal<IReferenceProviderFactory>(GetType().FullName!);
    }

    public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndexForChecks)
    {
        var project = sourceFile.GetProject();
        if (project == null || !project.IsUnityProject())
            return null;

        if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
            return new OdinMemberReferenceFactory(myCollector);

        return null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}