package com.jetbrains.rider.plugins.unity.ide.navigationToolbar

import com.intellij.ide.navigationToolbar.AbstractNavBarModelExtension
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml.UnityYamlFileType
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane

// This class hides .meta files from the navigation bar, and they can be shown again using the "show all files" setting
// in Solution Explorer or Unity Explorer.
// We don't hide folders such as Library, obj and Temp because the navbar is a view of the filesystem, rather than a
// view of the project model (in IntelliJ that's essentially the same thing). We could ignore these folders, but what
// should happen to other files/folders in the project root? We hide `.meta` because there is one for each file and
// folder, and they create a lot of noise.
// The navbar only lists files and folders that are not already excluded by the File Types | Ignored Files and Folders
// list. This means we don't see `.git`, but it also means `*~` is filtered, which impacts Unity's "hidden" folders,
// such as `Documentation~` (named like this so the asset database doesn't import the files). We can't override this
// setting (mainly because it's hardcoded, and a global setting, not per-project), so we will have some files shown in
// the Unity Explorer, but not shown in the navbar.
class UnityNavBarModelExtension : AbstractNavBarModelExtension() {
    override fun getPresentableText(o: Any?): String? = null

    override fun adjustElement(psiElement: PsiElement) =
        if (shouldHide(psiElement)) null else super.adjustElement(psiElement)

    private fun shouldHide(psiElement: PsiElement) = isMetaFile(psiElement) && !shouldShowMetaFiles(psiElement.project)

    private fun shouldShowMetaFiles(project: Project): Boolean {
        val solutionPane = SolutionExplorerViewPane.tryGetInstance(project) ?: return false

        return solutionPane.myShowAllFiles
    }

    private fun isMetaFile(psiElement: PsiElement): Boolean {
        if (psiElement is PsiFile) {
            return psiElement.fileType == UnityYamlFileType && psiElement.name.endsWith(".meta", false)
        }
        return false
    }
}