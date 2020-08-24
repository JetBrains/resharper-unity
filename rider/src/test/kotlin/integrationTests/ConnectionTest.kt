package integrationTests

import base.integrationTests.*
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.EditorLogEntry
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class ConnectionTest : IntegrationTestBase() {
    override fun getSolutionDirectoryName(): String = "SimpleUnityProjectWithoutPlugin"

    @Test
    fun installAndCheckConnectionAfterUnityStart() {
        withUnityProcess(false) {
            waitFirstScriptCompilation()
            installPlugin()
            waitConnection()
            checkSweaInSolution()
        }
    }

    @Test
    fun installAndCheckConnectionBeforeUnityStart() {
        installPlugin()
        withUnityProcess(false) {
            waitFirstScriptCompilation()
            waitConnection()
            checkSweaInSolution()
        }
    }

    @Test
    fun checkExternalEditorWithExecutingMethod() = checkExternalEditor(false) { executeIntegrationTestMethod("DumpExternalEditor") }

    @Test(enabled = false)
    fun checkExternalEditorWithUnityModelRefresh() = checkExternalEditor(true) { executeScript("DumpExternalEditor.cs") }

    private fun checkExternalEditor(resetEditorPrefs: Boolean, execute: () -> Unit) {
        installPlugin()
        withUnityProcess(resetEditorPrefs, true) {
            waitFirstScriptCompilation()
            waitConnection()

            val externalEditorPath = File(project.basePath, "Assets/ExternalEditor.txt")

            execute()
            waitAndPump(project.lifetime, { externalEditorPath.exists() }, defaultTimeout)
            { "ExternalEditor.txt is not created" }
            waitAndPump(project.lifetime, { externalEditorPath.readText().isNotEmpty() }, defaultTimeout)
            { "ExternalEditor.txt is empty" }

            executeWithGold(testGoldFile) {
                it.print(externalEditorPath.readText())
            }

            checkSweaInSolution()
        }
    }

    @Test
    fun checkLogWithExecutingMethod() = checkLog { executeIntegrationTestMethod("WriteToLog") }

    @Test(enabled = false)
    fun checkLogWithUnityModelRefresh() = checkLog { executeScript("WriteToLog.cs") }

    private fun checkLog(execute: () -> Unit) {
        installPlugin()
        withUnityProcess(false) {
            waitFirstScriptCompilation()
            waitConnection()

            var editorLogEntry: EditorLogEntry? = null
            rdUnityModel.onUnityLogEvent.adviseNotNullOnce(lifetime.createNested()) { entry ->
                editorLogEntry = entry
            }

            execute()
            waitAndPump(project.lifetime, { editorLogEntry != null }, defaultTimeout) { "Test message is not received" }

            executeWithGold(testGoldFile) {
                printEditorLogEntry(it, editorLogEntry!!)
            }

            checkSweaInSolution()
        }
    }
}
