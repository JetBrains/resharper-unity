package integrationTests

import base.integrationTests.*
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
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

            val externalEditorPath = File(project.basePath, "Assets/ExternalEditor.txt")

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

    @Test
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
