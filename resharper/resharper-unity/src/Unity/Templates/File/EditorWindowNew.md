---
guid: 3B84E43F-9A0B-42DA-AF15-8A17239F969B
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=EditorWindow, ValidateFileName=True
scopes: InUnityCSharpEditorFolder;MustBeInProjectWithUnityVersion(version=2022.2)
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE), MENUITEM, MENUITEMCOMMAND, TITLE
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# Editor Window

```
$HEADER$namespace $NAMESPACE$ {
  public class $CLASS$ : UnityEditor.EditorWindow
  {
    [UnityEditor.MenuItem("$MENUITEM$/$MENUITEMCOMMAND$")]
    private static void ShowWindow()
    {
      var window = GetWindow<$CLASS$>();
      window.titleContent = new UnityEngine.GUIContent("$TITLE$");
      window.Show();
    }

    private void CreateGUI()
    {
       $END$
    }
  }
}
```
