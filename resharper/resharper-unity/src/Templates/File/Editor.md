---
guid: 2E5D288C-A209-41EE-93B2-7CACDCAE18C6
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=MyComponentEditor, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (TYPE), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
TYPE-expression: getAlphaNumericFileNameWithoutSuffix("Editor1.cs")
NAMESPACE-expression: fileDefaultNamespace()
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
            base.OnInspectorGUI();
        }
    }
}
```
