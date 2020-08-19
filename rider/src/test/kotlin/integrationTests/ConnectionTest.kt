package integrationTests

import base.UnityIntegrationTestBase
import com.intellij.util.io.exists
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.EditorLogEntry
import com.jetbrains.rider.model.RunMethodData
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.checkSwea
import org.testng.annotations.Test
import java.io.File
import java.nio.file.Files
import java.nio.file.Paths

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class ConnectionTest : UnityIntegrationTestBase() {
    override fun getSolutionDirectoryName(): String = "SimpleUnityProjectWithoutPlugin"

    override fun preprocessTempDirectory(tempDir: File) {
        // Needed, because com.jetbrains.rider.plugins.unity.ProtocolInstanceWatcher
        //  isn't initialized without correct unity file structure
        val libraryFolder = Paths.get(tempDir.toString(), "Library")
        if (!libraryFolder.exists()) {
            Files.createDirectory(libraryFolder)
        }

        super.preprocessTempDirectory(tempDir)
    }

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
            rdUnityModel.onUnityLogEvent.adviseNotNull(lifetime) { entry ->
                editorLogEntry = entry
            }

            execute()
            waitAndPump(project.lifetime, { editorLogEntry != null }, defaultTimeout) { "Test message is not received" }

            executeWithGold(testGoldFile) {
                val type = RdLogEventType.values()[editorLogEntry!!.type]
                val mode = RdLogEventMode.values()[editorLogEntry!!.mode]
                if (type == RdLogEventType.Message) {
                    it.print("$type, $mode, ${editorLogEntry!!.message}\n\n" +
                        editorLogEntry!!.stackTrace.replace(Regex(" \\(at .+\\)"), ""))
                }
            }

            checkSweaInSolution()
        }
    }

    private fun checkSweaInSolution() {
        changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
        checkSwea(project)
    }

    private fun executeIntegrationTestMethod(methodName: String) =
        executeMethod(RunMethodData("Assembly-CSharp-Editor", "Editor.IntegrationTestHelper", methodName))
}
