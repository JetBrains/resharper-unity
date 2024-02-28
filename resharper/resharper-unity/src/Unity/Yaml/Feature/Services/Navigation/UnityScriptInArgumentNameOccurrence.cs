using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;

public class UnityScriptInArgumentNameOccurrence : UnityScriptsOccurrence, IAssetOccurrenceWithTextOccurrence
{
    public UnityScriptInArgumentNameOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement,
        LocalReference owningElementLocation, Guid guid, TextRange textRange) : base(sourceFile, declaredElement, owningElementLocation, guid)
    {
        RenameTextRange = textRange;
    }

    public IPsiSourceFile GetSourceFile()
    {
        return SourceFile;
    }

    public TextRange RenameTextRange { get; }
}