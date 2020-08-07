import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.checkSwea
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class ConnectionTest : UnityIntegrationTestBase() {
    @Test
    fun installAndCheckConnectionAfterUnityStart() {
        withUnityProcess(false) {
            waitFirstScriptCompilation()
            installPlugin()
            waitConnection()

            changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
            checkSwea(project)
        }
    }

    @Test
    fun installAndCheckConnectionBeforeUnityStart() {
        installPlugin()
        withUnityProcess(false) {
            waitFirstScriptCompilation()
            waitConnection()

            changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
            checkSwea(project)
        }
    }

    @Test
    fun checkExternalEditor() {
        installPlugin()
        withUnityProcess(resetEditorPrefs = true, useRiderTestPath = true) {
            waitFirstScriptCompilation()
            waitConnection()

            val externalEditorPath = File(project.basePath, "Assets/ExternalEditor.txt")

            executeScript("DumpExternalEditor.cs")
            waitAndPump(project.lifetime, { externalEditorPath.exists() && externalEditorPath.readText().isNotEmpty() },
                defaultTimeout) { "ExternalEditor.txt is not created" }

            executeWithGold(testGoldFile) {
                it.print(externalEditorPath.readText())
            }

            checkSwea(project)
        }
    }

    @Test
    fun checkLog() {
        installPlugin()
        withUnityProcess(true) {
            waitFirstScriptCompilation()
            waitConnection()

            executeWithGold(testGoldFile) {
                val definition = LifetimeDefinition()
                project.solution.rdUnityModel.onUnityLogEvent.adviseNotNull(definition.lifetime) { entry ->
                    val type = RdLogEventType.values()[entry.type]
                    val mode = RdLogEventMode.values()[entry.mode]
                    if (type == RdLogEventType.Message) {
                        it.print("$type, $mode, ${entry.message}\n\n${entry.stackTrace.replace(Regex(" \\(at .+\\)"), "")}")

                        if (entry.message == "#Test#")
                            definition.terminate()
                    }
                }

                executeScript("WriteToLog.cs")
                waitAndPump(project.lifetime, { !definition.isAlive }, defaultTimeout) { "Test message is not received" }

                checkSwea(project)
            }
        }
    }
}
