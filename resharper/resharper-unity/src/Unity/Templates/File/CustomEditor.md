---
guid: 2E5D288C-A209-41EE-93B2-7CACDCAE18C6
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=CustomEditor, ValidateFileName=True
scopes: UnityFileTemplateSectionMarker;InUnityCSharpEditorFolder;MustBeInProjectWithMaximumUnityVersion(version=2022.1)
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE), TYPE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TYPE-expression: complete()
---

# Custom Editor (IMGUI)

```
$HEADER$namespace $NAMESPACE$ {
  [UnityEditor.CustomEditor(typeof($TYPE$))]
  public class $CLASS$ : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      $END$
      base.OnInspectorGUI();
    }
  }
}
```
