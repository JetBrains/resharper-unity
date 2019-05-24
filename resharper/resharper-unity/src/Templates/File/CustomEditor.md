---
guid: 2E5D288C-A209-41EE-93B2-7CACDCAE18C6
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=CustomEditor, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE), TYPE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TYPE-expression: complete()
---

# Custom Editor

```
$HEADER$using UnityEditor;

namespace $NAMESPACE$ {
  [CustomEditor(typeof($TYPE$))]
  public class $CLASS$ : Editor
  {
    public override void OnInspectorGUI() 
    {
      $END$
      base.OnInspectorGUI();
    }
  }
}
```
