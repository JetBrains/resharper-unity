---
guid: D4BC1DCD-C297-4DA9-8072-03CCBA27C34C
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=CustomEditor, ValidateFileName=True
scopes: InUnityCSharpEditorFolder;MustBeInProjectWithUnityVersion(version=2022.2)
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE), TYPE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TYPE-expression: complete()
---

# Custom Editor

```
$HEADER$namespace $NAMESPACE$ {
  [UnityEditor.CustomEditor(typeof($TYPE$))]
  public class $CLASS$ : UnityEditor.Editor
  {
    public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
    {
      $END$
      return base.CreateInspectorGUI();
    }
  }
}
```
