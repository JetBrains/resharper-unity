import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.framework.*
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

class UnityClassLibTest : BaseTestWithSolutionBase() {

    private val templateId = TemplateIdWithVersion("JetBrains.Common.Unity.Library.CSharp", CoreVersion.NONE)
    private val editorGoldFile: File
        get() = File(testCaseGoldDirectory, "${testMethod.name}_opened")

    @Test
    fun testUnityClassLibraryTemplate() {
        val params = OpenSolutionParams()
        params.restoreNuGetPackages = true //it's always true in getAndOpenSolution
        params.waitForCaches = true

        val lifetimeDef = Lifetime.Eternal.createNested()
        try {
            val newProject = getAndOpenSolution(lifetimeDef.lifetime, templateId, true, params)

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
        } finally {
            lifetimeDef.terminate()
        }
    }

    private fun getAndOpenSolution(
            lifetime: Lifetime,
            templateId: TemplateIdWithVersion,
            sameDirectory: Boolean,
            params: OpenSolutionParams
    ): Project {
        closeProjectsWaitForBackendWillBeClosed(Duration.ofSeconds(60), false, false)
        val parameters: HashMap<String, String> = hashMapOf()

        VfsRootAccess.allowRootAccess(lifetime.createNestedDisposable(), testDirectory.combine("lib", "UnityEngine.dll").absolutePath)
        parameters["PathToUnityEngine"] = testDirectory.combine("lib", "UnityEngine.dll").absolutePath
        val newProject = createSolutionFromTemplate(templateId, null, activeSolutionDirectory, sameDirectory, null, parameters) { }!!

        newProject.enableBackendAsserts()
        persistAllFilesOnDisk()

        waitForSolution(newProject, params)
        assertCurrentSolutionToolset(newProject)

        return newProject
    }
}