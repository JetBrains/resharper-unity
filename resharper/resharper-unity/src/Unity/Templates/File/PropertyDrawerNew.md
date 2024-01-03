---
guid: C04349FF-EFAC-4468-B3CA-0D1159DC8482
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=PropertyDrawer, ValidateFileName=True
scopes: InUnityCSharpEditorFolder;MustBeInProjectWithUnityVersion(version=2022.2)
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE), TYPE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TYPE-expression: complete()
---

# Property Drawer

```
$HEADER$namespace $NAMESPACE$ {
  [UnityEditor.CustomPropertyDrawer(typeof($TYPE$))]
  public class $CLASS$ : UnityEditor.PropertyDrawer
  {
    public override UnityEngine.UIElements.VisualElement CreatePropertyGUI(UnityEditor.SerializedProperty property)
    {
      $END$
      return base.CreatePropertyGUI(property);
    }
  }
}
```
