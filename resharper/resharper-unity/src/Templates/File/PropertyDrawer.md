---
guid: 7901AA8B-4060-4763-8FD5-B7B5384FABAA
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=PropertyDrawer, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE), TYPE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TYPE-expression: complete()
---

# Property Drawer

```
$HEADER$using UnityEngine;
using UnityEditor;

namespace $NAMESPACE$ {
  [CustomPropertyDrawer(typeof($TYPE$))]
  public class $CLASS$ : PropertyDrawer
  {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      $END$
    }
  }
}
```
