package integrationTests

import base.integrationTests.*
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL]) // todo: allow Linux
class ConnectionTest : IntegrationTestWithSolutionBase() {
    override fun getSolutionDirectoryName(): String = "SimpleUnityProjectWithoutPlugin"

    @Test
    fun installAndCheckConnectionAfterUnityStart() {
        withUnityProcess {
            waitFirstScriptCompilation(project)
            installPlugin()
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test
    fun installAndCheckConnectionBeforeUnityStart() {
        installPlugin()
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)
            checkSweaInSolution()
        }
    }

    @Test
    fun checkExternalEditorWithExecutingMethod() = checkExternalEditor(false) {
        executeIntegrationTestMethod("DumpExternalEditor")
    }

    @Test(enabled = false)
    fun checkExternalEditorWithUnityModelRefresh() = checkExternalEditor(true) { executeScript("DumpExternalEditor.cs") }

    private fun checkExternalEditor(resetEditorPrefs: Boolean, execute: () -> Unit) {
        installPlugin()
        withUnityProcess(resetEditorPrefs = resetEditorPrefs, useRiderTestPath = true) {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)

            val externalEditorPath = project.solutionDirectory.resolve( "Assets/ExternalEditor.txt")

            execute()
            waitAndPump(project.lifetime, { externalEditorPath.exists() }, unityDefaultTimeout)
            { "ExternalEditor.txt is not created" }
            waitAndPump(project.lifetime, { externalEditorPath.readText().isNotEmpty() }, unityDefaultTimeout)
            { "ExternalEditor.txt is empty" }

            executeWithGold(testGoldFile) {
                it.print(externalEditorPath.readText())
            }

            checkSweaInSolution()
        }
    }

    @Test
    fun checkLogWithExecutingMethod() = checkLog { executeIntegrationTestMethod("WriteToLog") }

    @Test
    fun checkLogWithUnityModelRefresh() = checkLog { executeScript("WriteToLog.cs") }

    private fun checkLog(execute: () -> Unit) {
        installPlugin()
        withUnityProcess {
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)

            val editorLogEntry = waitForEditorLogsAfterAction("#Test#") { execute() }.first()
            executeWithGold(testGoldFile) {
                printEditorLogEntry(it, editorLogEntry)
            }

            checkSweaInSolution()
        }
    }

    // TODO: test reproduce bug only with dialog with info about wrong unity version,
    //  but we can't terminate Unity Editor with UI before connection
    @Test(description = "RIDER-52498",enabled = false)
    fun checkDebuggerStartsAfterAttachDebugger() {
        installPlugin()
        try {
//            startUnity(false, false, false ,true)
//            waitFirstScriptCompilation(project)
//            waitConnectionToUnityEditor(project)
            attachDebuggerToUnityEditor(
                {
                //    replaceUnityVersionOnCurrent(project)
                },
                {
                    waitConnectionToUnityEditor(project)
                }
            )
        } finally {
            killUnity(project)
            checkSweaInSolution()
        }
    }
}
