package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RefactoringsTestBase
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.callAltEnterMenu
import com.jetbrains.rider.test.scriptingApi.executeItemByPrefix
import com.jetbrains.rider.test.scriptingApi.waitBackendDocumentChange
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class QuickFixProjectSettingsTest : RefactoringsTestBase() {
    override fun getSolutionDirectoryName(): String = "ProjectSettingsTestData"
    override val editorGoldFile: File
        get() = File(testCaseGoldDirectory,  testMethod.name)

    @Test
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

    @Test
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

    @Test
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
    fun initializeEnvironment() {
        prepareAssemblies(project, activeSolutionDirectory)
    }
}