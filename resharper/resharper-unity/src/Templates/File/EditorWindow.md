---
guid: CA9DFEEA-D5B5-4DDC-933F-8D618D71538E
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=MyEditorWindow, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
parameterOrder: HEADER, (CLASS), (NAMESPACE), (TITLE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension
NAMESPACE-expression: fileDefaultNamespace()
TITLE-expression: getAlphaNumericFileNameWithoutSuffix("Window1")
---

# Editor Window

```
$HEADER$using UnityEditor;

namespace $NAMESPACE$ {
    public class $CLASS$ : EditorWindow
    {
        [MenuItem("$TITLE$")]
        private static void ShowWindow() 
        {
            var window = GetWindow<$CLASS$>();
            window.titleContent = new GUIContent("$TITLE$");
            window.Show();
        }
    
        private void OnGUI() 
        {
            
        }
    }
}
```
