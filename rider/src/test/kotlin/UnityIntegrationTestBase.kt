import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.util.idea.lifetime
import java.nio.file.Path
import java.nio.file.Paths
import kotlin.test.assertNotNull

open class UnityIntegrationTestBase : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String = "SimpleUnityProjectWithoutPlugin"
    override val waitForCaches = true
    protected fun getUnityPath() : String = "C:/Program Files/Unity"

    var unityPackedUrl = when{
        SystemInfo.isWindows -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1_stripped_v4.zip"
        SystemInfo.isMac -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1.tar.gz"
        else -> throw Exception("Not implemented")
    }

    fun startUnity(resetEditorPrefs : Boolean) : Process? {
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

        val args = mutableListOf("-logfile", logPath.toString(), "-batchMode", "-silent-crashes",
            "-riderTests")
        if (resetEditorPrefs) {
            args.add("-executeMethod")
            args.add("Editor.IntegrationTestHelper.ResetAndStart")
        } else {
            args.add("-executeMethod")
            args.add("Editor.IntegrationTestHelper.Start")
        }

        if (isRunningInTeamCity)
        {
            val login = System.getenv("login")
            val password = System.getenv("password")
            assertNotNull(login, "System.getenv(\"login\") is null.")
            assertNotNull(password, "System.getenv(\"password\") is null.")
            args.addAll(arrayOf("-username", login, "-password", password))
        }
        return StartUnityAction.startUnity(appPath, project, args.toTypedArray())
    }

    fun waitFirstScriptCompilation() {
        val unityStartFile = Paths.get(project.basePath).resolve(".start")
        waitAndPump(project.lifetime, { unityStartFile.exists() }, 120000, { "Unity was not started." })
    }

    fun installPlugin() {
        val unityHost = UnityHost.getInstance(project)
        unityHost.model.installEditorPlugin.fire(Unit)

        val editorPluginPath = Paths.get(project.basePath).resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")
        waitAndPump(project.lifetime, { editorPluginPath.exists() }, 10000, { "EditorPlugin was not installed." })
    }

    fun waitConnection() {
        val unityHost = UnityHost.getInstance(project)
        waitAndPump(project.lifetime, { unityHost.sessionInitialized.hasTrueValue }, 100000, { "unityHost is not initialized." })
    }

    fun killUnity(process : Process) {
        process.destroy()
        waitAndPump(project.lifetime, { !process.isAlive }, 100000, { "Process should have existed." })
    }

    fun executeScript(file : String) {
        val script = solutionSourceRootDirectory.combine("scripts", file)
        script.copyTo(activeSolutionDirectory.combine("Assets", file))
        val unityHost = UnityHost.getInstance(project)
        unityHost.model.refresh.fire(true)
    }
}