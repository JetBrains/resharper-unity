---
guid: CA9DFEEA-D5B5-4DDC-933F-8D618D71538E
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=EditorWindow, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
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

    private void OnGUI()
    {
      $END$
    }
  }
}
```
