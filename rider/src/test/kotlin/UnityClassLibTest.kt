import com.intellij.openapi.project.Project
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.framework.closeProjectsWaitForBackendWillBeClosed
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

class UnityClassLibTest : BaseTestWithSolutionBase() {

    var templateId = "JetBrains.Common.Unity.Library.CSharp"
    private val editorGoldFile: File
        get() = File(testCaseGoldDirectory, "${testMethod.name}_opened")

    @Test
    fun testXamarinFormsClassLibraryTemplate() {
        val projectName = "ClassLibrary"
        val params = OpenSolutionParams()
        params.restoreNuGetPackages = true //it's always true in getAndOpenSolution
        params.waitForCaches = true

        val newProject = getAndOpenSolution(templateId, true, params)

        try {
            executeWithGold(editorGoldFile) {
                dumpOpenedDocument(it, newProject)
            }

            testProjectModel(testGoldFile, newProject) {
                dump("Opened", newProject, activeSolutionDirectory, false, false) {} //contains close editors
            }

            checkSwea(newProject)

        } finally {
            closeSolutionAndResetSettings(newProject)
        }
    }

    private fun getAndOpenSolution(
            templateId: String,
            sameDirectory: Boolean,
            params: OpenSolutionParams
    ): Project {
        closeProjectsWaitForBackendWillBeClosed(60, false)
        val newProject = createSolutionFromTemplate(templateId, null, activeSolutionDirectory, sameDirectory) { solutionFile ->

        }!!

        newProject.enableBackendAsserts()
        persistAllFilesOnDisk(newProject)

        waitForSolution(newProject, params)
        assertCurrentSolutionToolset()

        return newProject
    }
}