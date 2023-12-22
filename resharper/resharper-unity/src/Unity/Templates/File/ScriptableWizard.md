---
guid: E1BD73A0-0145-4A1A-B4AA-7744144744AF
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=ScriptableWizard, ValidateFileName=True
scopes: InUnityCSharpEditorFolder
uitag: Unity Script
parameterOrder: HEADER, (CLASS), (NAMESPACE), MENUITEM, MENUITEMCOMMAND, TITLE, CREATE, OTHER
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
MENUNAME-expression: complete()
TITLE-expression: complete()
CREATE-expression: complete()
OTHER-expression: complete()
---

# Scriptable Wizard

```
$HEADER$namespace $NAMESPACE$ {
  public class $CLASS$ : UnityEditor.ScriptableWizard
  {
    [UnityEditor.MenuItem("$MENUITEM$/$MENUITEMCOMMAND$")]
    public static void CreateWizard()
    {
        DisplayWizard<$CLASS$>("$TITLE$", "$CREATE$", "$OTHER$");
    }

    public void OnWizardCreate()
    {
        $END$
    }

    public void OnWizardUpdate()
    {

    }

    public void OnWizardOtherButton()
    {

    }
  }
}
```
