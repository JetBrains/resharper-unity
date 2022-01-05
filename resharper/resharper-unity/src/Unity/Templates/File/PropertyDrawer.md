---
guid: 7901AA8B-4060-4763-8FD5-B7B5384FABAA
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=PropertyDrawer, ValidateFileName=True
scopes: UnityFileTemplateSectionMarker;InUnityCSharpEditorFolder
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
    public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label)
    {
      $END$
    }

    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, UnityEngine.GUIContent label)
    {
      return base.GetPropertyHeight(property, label);
    }
  }
}
```
