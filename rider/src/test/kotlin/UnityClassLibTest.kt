import com.intellij.openapi.project.Project
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.framework.closeProjectsWaitForBackendWillBeClosed
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

class UnityClassLibTest : BaseTestWithSolutionBase() {

    var templateId = "JetBrains.Common.Unity.Library.CSharp"
    private val editorGoldFile: File
        get() = File(testCaseGoldDirectory, "${testMethod.name}_opened")

    @Test
    fun testUnityClassLibraryTemplate() {
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

            // todo: fix UnityEngine.dll reference - either install Unity or from nuget
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
        closeProjectsWaitForBackendWillBeClosed(Duration.ofSeconds(60), false, false)
        val parameters: HashMap<String, String> = hashMapOf()
        parameters["PathToUnityEngine"] = testDirectory.combine("lib", "UnityEngine.dll").absolutePath
        val newProject = createSolutionFromTemplate(templateId, null, activeSolutionDirectory, sameDirectory, null, parameters) { }!!

        newProject.enableBackendAsserts()
        persistAllFilesOnDisk(newProject)

        waitForSolution(newProject, params)
        assertCurrentSolutionToolset()

        return newProject
    }
}