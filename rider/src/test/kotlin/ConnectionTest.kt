import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.scriptingApi.changeFileSystem2
import com.jetbrains.rider.test.scriptingApi.checkSwea
import com.jetbrains.rider.util.idea.lifetime
import org.testng.annotations.Test
import java.io.File
import java.nio.file.Path
import java.nio.file.Paths
import kotlin.test.assertNotNull

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS]) // todo: allow Linux
class ConnectionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "SimpleUnityProject"
    }

    override val waitForCaches = true;

    var unityPackedUrl = when{
        SystemInfo.isWindows -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1_stripped_v4.zip"
        SystemInfo.isMac -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1.tar.gz"
        else -> throw Exception("Not implemented")
    }

    @Test
    fun test() {


        VfsRootAccess.allowRootAccess("C:/Program Files/Unity")

        val editorPluginPath = Paths.get(project.basePath).resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")

        waitAndPump(project.lifetime, { editorPluginPath.exists()}, 10000, { "EditorPlugin was not installed."})

        val logPath = Paths.get(project.basePath).resolve("Editor.log")

        val appPath: Path
        val isRunningInTeamCity = TeamCityHelper.isUnderTeamCity
        if (isRunningInTeamCity) // on teamcity download Unity
        {
            val folder = downloadAndExtractArchiveArtifactIntoPersistentCache(unityPackedUrl)
            appPath = when {
                SystemInfo.isWindows -> folder.combine("Unity.exe").toPath()
                SystemInfo.isMac -> folder.toPath()
                else -> throw Exception("Not implemented")
            }
        }
        else
        {
            val localAppPath = UnityInstallationFinder.getInstance(project).getApplicationPath()
            assertNotNull(localAppPath, "Unity installation was not found.")
            appPath = localAppPath
        }

        val args = mutableListOf(appPath.toString(), project.basePath.toString(),
            "-logfile", logPath.toString(), "-batchMode", "-quit", "-silent-crashes",
            "\"-executeMethod\"", "\"JetBrains.Rider.Unity.Editor.Internal.RiderTests.EnableLogsSyncSolution\"",
            "-riderTests")
        if (isRunningInTeamCity)
        {
            val login = System.getenv("login")
            val password = System.getenv("password")
            assertNotNull(login, "System.getenv(\"login\") is null.")
            assertNotNull(password, "System.getenv(\"password\") is null.")
            args.addAll(arrayOf("-username", login, "-password", password))
        }
        val process = StartUnityAction.startUnity(args)
        assertNotNull(process)

        val unityHost = UnityHost.getInstance(project)
        waitAndPump(project.lifetime, {unityHost.sessionInitialized.hasTrueValue}, 100000, {"unityHost is not initialized."})

        waitAndPump(project.lifetime, {!process.isAlive}, 100000, {"Process should have existed."})

        val projectVirtualFile = File(project.basePath).combine("Assembly-CSharp.csproj")
        changeFileSystem2(project){ arrayOf(projectVirtualFile) }

        checkSwea(project)

        process.destroyForcibly()
    }
}
