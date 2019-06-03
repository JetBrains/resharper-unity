---
guid: CA9DFEEA-D5B5-4DDC-933F-8D618D71538E
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=EditorWindow, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE), MENUITEM, TITLE 
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
---

# Editor Window

```
$HEADER$using UnityEngine;
using UnityEditor;

namespace $NAMESPACE$ {
  public class $CLASS$ : EditorWindow
  {
    [MenuItem("$MENUITEM$")]
    private static void ShowWindow() 
    {
      var window = GetWindow<$CLASS$>();
      window.titleContent = new GUIContent("$TITLE$");
      window.Show();
    }

    private void OnGUI() 
    {
      $END$
    }
  }
}
```
