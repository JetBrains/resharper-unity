using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    // See https://docs.unity3d.com/Manual/FormatDescription.html for a description of the .unity YAML format
    // Useful bits of knowledge:
    // * Each object is stored as a separate YAML document inside the file
    // * Each document's root node has a tag property of `!u!` followed by a number. `!u!` is a reference to the tag
    //   definition: `%TAG !u! tag:unity3d.com,2011:`. The number is the class of the object (see
    //   https://docs.unity3d.com/Manual/ClassIDReference.html for values)
    // * Each document's root node has an anchor property. This number is unique to the file, and can be used to refer
    //   back to the document by {fileID: 000000}. The number has no significance or stability
    // * Get to know the fileID structure. This is a YAML flow mapping, e.g. {fileID: 11500000} for an internal fileID
    //   or {fileID: 1297475563, guid: f5f67c52d1564df4a8936ccd202a3bd8, type: 3}
    // * The fileID property refers to the anchor property of a document in a file. If there is no guid, it's for the
    //   current file. If a guid property is specified, this is the guid of the asset, taken from the .meta file. In
    //   this case, fileID refers to the anchor property of a document in that asset file
    // * The scene hierarchy comes from the RectTransform types. The m_Children property is a sequence of file IDs that
    //   link to the child RectTransform. The m_Father property is the (sexist) parent transform
    // * Each RectTransform has an m_GameObject fileID property which links to the associated GameObject
    // * Each GameObject has an m_Component property that is a sequence of fileIDs that link to the GameObject's
    //   components, which includes the RectTransform and any MonoBehaviours
    // * Each MonoBehaviour has a link back to the GameObject with an m_GameObject fileID
    // * Each MonoBehaviour also has an m_Script property that links either to a C# file, or to a type in an assembly.
    //   This property uses an external fileID. The guid refers to the asset. If the asset is a C# file, the fileID
    //   itself is the well known value of 11500000 (115 is the class ID for MonoScript). If the asset is a .dll the
    //   fileID is a has of "s\0\0\0" + Namespace + Name (no dot, and where "s\0\0\0" is 115 as a 32 bit integer)
    //   See https://forum.unity.com/threads/yaml-fileid-hash-function-for-dll-scripts.252075/
    // * UnityEvent based registrations serialise as a map with two entries m_PersistentCalls and m_TypeName (the
    //   serialised fields of UnityEventBase). The m_TypeName property is the type of the serialised event. The handlers
    //   are serialised as a list of PersistentCalls, with m_Target being the MonoScript fileID and m_MethodName being
    //   the name of the method in that type (so be careful with rename!)
    //[ReferenceProviderFactory]
    public class UnityEventTargetReferenceProviderFactory : IReferenceProviderFactory
    {
        public UnityEventTargetReferenceProviderFactory(Lifetime lifetime)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
        }

        public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            if (sourceFile.PrimaryPsiLanguage.Is<UnityYamlLanguage>() && sourceFile.IsAsset())
            {
                if (wordIndexForChecks == null || wordIndexForChecks.CanContainAllSubwords(sourceFile, "m_MethodName"))
                    return new UnityEventTargetReferenceFactory();
            }

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}