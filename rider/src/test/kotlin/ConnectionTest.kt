import com.intellij.util.io.exists
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.checkSwea
import com.jetbrains.rider.util.idea.lifetime
import org.testng.annotations.Test
import java.io.File
import java.nio.file.Paths
import kotlin.test.assertNotNull

class ConnectionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "SimpleUnityProject"
    }

    override val waitForCaches = true;

    var unityPackedUrl = "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1_stripped_v4.zip";

    @Test(enabled = false)
    fun test() {

        val editorPluginPath = Paths.get(project.basePath).resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")

        waitAndPump(project.lifetime, { editorPluginPath.exists()}, 10000, { "EditorPlugin was not installed."})

        val logPath = Paths.get(project.basePath).resolve("Editor.log")
        val root = downloadAndExtractArchiveArtifactIntoPersistentCache(unityPackedUrl)
        val args =
            arrayOf("-logfile", "\"$logPath\"", "-batchMode", "-quit", "-silent-crashes","-username", "\"rider-unity@jetbrains.com\"",
                "-password", "\"Rider-unity1\"", "\"-executeMethod\"", "\"JetBrains.Rider.Unity.Editor.Internal.RiderTests.EnableLogsSyncSolution\"",
                "-riderTests")
        val process = StartUnityAction.StartUnity(root.combine("Unity.exe").toPath(), project, args)
        assertNotNull(process)

        val unityHost = UnityHost.getInstance(project)
        waitAndPump(project.lifetime, {unityHost.sessionInitialized.hasTrueValue}, 100000, {"unityHost is not initialized."})

        waitAndPump(project.lifetime, {!process.isAlive}, 100000, {"Process should have existed."})

        val projectVirtualFile = File(project.basePath).combine("Assembly-CSharp.csproj")
        changeFileSystem2(project){ arrayOf(projectVirtualFile) }

        // todo: fix UnityEngine.dll reference - either install Unity or from nuget
        checkSwea(project)
    }
}
