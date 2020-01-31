using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    // Assets can contain MonoBehaviour instances, and each MonoBehaviour has a reference to a MonoScript instance that
    // describes how to locate the script class that provides the functionality for the behaviour. In the YAML, this is
    // represented by the m_Script member of the MonoBehaviour instance (!u!114). This member contains a fileID
    // structure, where the guid identifies the script asset, and the fileID itself is a value relative to the script
    // asset. For precompiled scripts, the asset is a .dll, and the fileID is a hash that identifies a class within the
    // .dll. For loose scripts (i.e. .cs files) the fileID is irrelevant, and the static value 11500000 is used instead
    // (115 is the class ID for MonoScript). Unity expects to find a class with the same name as the file, possibly in a
    // namespace. Behaviour is undefined if there are multiple classes with the same name in a file (they would have to
    // have different namespaces)
    // We'll add a reference from the guid of a 11500000 fileID structure to the class it represents, by finding the C#
    // file asset with the same guid, and the class of the same name inside the file
    [ReferenceProviderFactory]
    public class MonoScriptReferenceProviderFactory : IReferenceProviderFactory
    {
        public MonoScriptReferenceProviderFactory(Lifetime lifetime)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            if (sourceFile.PrimaryPsiLanguage.Is<UnityYamlLanguage>() && sourceFile.IsAsset())
            {
                return new MonoScriptReferenceFactory();
            }

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}