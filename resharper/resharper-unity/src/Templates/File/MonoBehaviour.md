---
guid: 5ff5ac38-7207-4256-91ae-b5436552db13
image: UnityCSharp
type: File
reformat: True
shortenReferences: True
categories: unity
customProperties: Extension=cs, FileName=MonoBehaviour, ValidateFileName=True
scopes: UnityFileTemplateSectionMarker;InUnityCSharpProject
uitag: Unity Class
parameterOrder: HEADER, (CLASS), (NAMESPACE)
HEADER-expression: fileheader()
CLASS-expression: getAlphaNumericFileNameWithoutExtension()
NAMESPACE-expression: fileDefaultNamespace()
---

# Mono Behaviour

```
$HEADER$namespace $NAMESPACE$ {
  public class $CLASS$ : UnityEngine.MonoBehaviour {$END$}
}
```
