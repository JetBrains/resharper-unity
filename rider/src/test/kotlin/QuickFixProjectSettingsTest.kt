import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.base.RefactoringsTestBase
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.BeforeSuite
import org.testng.annotations.Test
import java.io.File

class QuickFixProjectSettingsTest : RefactoringsTestBase() {

    lateinit var unityDll : File

    @BeforeSuite(alwaysRun = true)
    fun getUnityDll() {
        unityDll = DownloadUnityDll()
    }

    override fun getSolutionDirectoryName(): String = "ProjectSettingsTestData"
    override val editorGoldFile: File
        get() = File(testCaseGoldDirectory,  testMethod.name)

    @Test(enabled = false)
    fun testAddToBuildSettings() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
            FrontendTextControlHost.getInstance(project!!)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))
            callAltEnterMenu {
                executeItemByPrefix("Add 'Scenes/PossibleShortName' to build settings")
            }
        }

        writeProjectSettingsToGold()
    }

    @Test(enabled = false)
    fun testEnableSceneAtBuildSettings() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
            FrontendTextControlHost.getInstance(project!!)
            waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))
            callAltEnterMenu {
                executeItemByPrefix("Enable scene")
            }
        }

        writeProjectSettingsToGold()
    }

    @Test(enabled = false)
    fun testSpecifyFullSceneName() {
        doTestWithDumpDocument {
            withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
                FrontendTextControlHost.getInstance(project!!)
                waitBackendDocumentChange(project!!, arrayListOf(this.virtualFile))
                callAltEnterMenu {
                    executeItemByPrefix("Change scene name to 'Scenes/Folder/ImpossibleShortName'")
                }
            }
        }
    }

    private fun getProjectSettingsFromSolution(solutionFolder : File) : File {
        return solutionFolder.combine("ProjectSettings", "EditorBuildSettings.asset")
            .combine()
    }

    private fun writeProjectSettingsToGold() {
        executeWithGold(editorGoldFile) {
            it.print(getProjectSettingsFromSolution(activeSolutionDirectory).readText())
        }
    }

    @BeforeMethod
    fun InitializeEnvironement() {
        CopyUnityDll(unityDll, project, activeSolutionDirectory)
    }
}