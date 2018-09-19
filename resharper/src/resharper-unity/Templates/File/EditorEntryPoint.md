---
guid: DA24767F-E6BB-4463-ACB4-799D7CE68822
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=EditorEntryPoint, ValidateFileName=True
scopes: InUnityCSharpProject
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Editor EntryPoint C# script.

```
$HEADER$using UnityEditor;

namespace $NAMESPACE$ {
    [InitializeOnLoad]
    public static class $CLASS$
    {
        static $CLASS$()
        {$END$}
    }
}
```
