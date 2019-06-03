---
guid: B669492E-B3A6-4F98-9998-9AF480374340
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=AssetPostprocessor, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# Asset Postprocessor

```
$HEADER$using UnityEngine;
using UnityEditor;

namespace $NAMESPACE$ {
  public class $CLASS$ : AssetPostprocessor
  {
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
      $END$
    }
  }
}
```
