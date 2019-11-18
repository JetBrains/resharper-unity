<h3>Action "Refresh Unity Assets"</h3>
It is specifically useful to debug [startup code in Unity](https://docs.unity3d.com/Manual/RunningEditorCodeOnLaunch.html)

It causes AppDomain reload, regeneration of sln/csproj files and `AssetDatabase.Refresh`

<h3>Refresh triggered automatically</h3>
This type of refresh causes regeneration of sln/csproj files and `AssetDatabase.Refresh` in case we are not in PlayMode and `AutoRefresh` is enabled in Unity.
It may be triggered by one of the following reasons:

1. User calls SaveAll/SaveDocument actions in Rider
2. `cs` file is added or deleted inside the SolutionFolder (implemented using `RecursiveFileSystemChangeDeltaVisitor`)
3. `solution.GetProtocolSolution().Editors.AfterDocumentInEditorSaved` is fired. (by refactoring)

In case when new signals Refresh1, Refresh2, RefreshN are triggered, while Refresh0 is being executed, only one Refresh will get started after Refresh0 has finished.
