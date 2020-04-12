import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.intellij.util.io.readText
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
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.checkSwea
import java.io.File
import java.nio.file.Paths
import java.time.Duration
import kotlin.test.assertNotNull

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class ConnectionTest : UnityIntegrationTestBase() {


    override fun preprocessTempDirectory(tempDir: File) {
        VfsRootAccess.allowRootAccess(getUnityPath())
    }

    // @Test
    fun installAndCheckConnectionAfterUnityStart() {

        val process = startUnity(false)
        try {
            assertNotNull(process)

            waitFirstScriptCompilation()
            installPlugin()
            waitConnection()
            killUnity(process)

            val projectVirtualFile = File(project.basePath).combine("Assembly-CSharp.csproj")
            changeFileSystem2(project) { arrayOf(projectVirtualFile) }

            checkSwea(project)
        } finally {
            if (process?.isAlive == true)
                process.destroy()
        }
    }

    // @Test
    fun installAndCheckConnectionBeforeUnityStart() {
        installPlugin()
        val process = startUnity(false)
        try {
            assertNotNull(process)

            waitFirstScriptCompilation()
            waitConnection()
            killUnity(process)

            val projectVirtualFile = File(project.basePath).combine("Assembly-CSharp.csproj")
            changeFileSystem2(project) { arrayOf(projectVirtualFile) }

            checkSwea(project)
        } finally {
            if (process?.isAlive == true)
                process.destroy()
        }
    }

    // @Test
    fun checkExternalEditor() {
        installPlugin()
        val process = startUnity(true)
        try {
            assertNotNull(process)

            waitFirstScriptCompilation()
            waitConnection()

            val externalEditorPath = Paths.get(project.basePath).resolve("Assets/ExternalEditor.txt")

            executeScript("DumpExternalEditor.cs")
            waitAndPump(project.lifetime, { externalEditorPath.exists() }, Duration.ofSeconds(100), { "ExternalEditor.txt is not created" })

            executeWithGold(testGoldFile) {
                it.print(externalEditorPath.readText())
            }

            killUnity(process)
            checkSwea(project)
        } finally {
            if (process?.isAlive == true)
                process.destroy()
        }
    }

    // @Test
    fun checkLog() {
        installPlugin()
        val process = startUnity(true)
        try {
            assertNotNull(process)

            waitFirstScriptCompilation()
            waitConnection()

            executeScript("WriteToLog.cs")

            executeWithGold(testGoldFile) {
                val model = project.solution.rdUnityModel
                val definition = LifetimeDefinition()
                model.onUnityLogEvent.adviseNotNull(definition.lifetime) {entry ->
                    val type = RdLogEventType.values()[entry.type]
                    val mode = RdLogEventMode.values()[entry.mode]
                    if (type == RdLogEventType.Message) {
                        it.print("$type, $mode, ${entry.message}, ${entry.stackTrace}")

                        if (entry.message == "#Test#")
                            definition.terminate()
                    }
                }

                waitAndPump(project.lifetime, { !definition.isAlive }, Duration.ofSeconds(100), { "Test message is not received" })

                killUnity(process)
                checkSwea(project)
            }
        } finally {
            if (process?.isAlive == true)
                process.destroy()
        }
    }
}
