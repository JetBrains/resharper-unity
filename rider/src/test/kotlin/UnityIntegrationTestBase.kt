import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.reactive.hasTrueValue
import com.jetbrains.rdclient.util.idea.callSynchronously
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.model.RunMethodData
import com.jetbrains.rider.model.RunMethodResult
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.protocol.protocol
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.framework.frameworkLogger
import org.testng.annotations.AfterClass
import java.io.File
import java.nio.file.Paths
import java.time.Duration
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

abstract class UnityIntegrationTestBase : BaseTestWithSolution() {

    companion object {
        val defaultTimeout: Duration = Duration.ofSeconds(120)
    }

    private val lifetimeDefinition = LifetimeDefinition()
    val lifetime = lifetimeDefinition.lifetime

    override val waitForCaches = true
    override fun preprocessTempDirectory(tempDir: File) {
        VfsRootAccess.allowRootAccess(lifetimeDefinition.createNestedDisposable(), unityPath)
    }

    protected val rdUnityModel: RdUnityModel
        get() = project.solution.rdUnityModel

    private val unityPath = when {
        SystemInfo.isWindows -> "C:/Program Files/Unity"
        SystemInfo.isMac -> "/Applications/Unity"
        else -> throw Exception("Not implemented")
    }

    private val unityPackedUrl = when {
        SystemInfo.isWindows -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1_stripped_v4.zip"
        SystemInfo.isMac -> "https://repo.labs.intellij.net/dotnet-rider-test-data/Unity_2018.3.4f1.tar.gz"
        else -> throw Exception("Not implemented")
    }

    private fun startUnity(resetEditorPrefs: Boolean, useRiderTestPath: Boolean): Process {
        val logPath = testMethod.logDirectory.resolve("UnityEditor.log")

        val isRunningInTeamCity = TeamCityHelper.isUnderTeamCity

        if (isRunningInTeamCity) { // on teamcity download Unity
            frameworkLogger.info("Downloading unity from $unityPackedUrl")
            downloadAndExtractArchiveArtifactIntoPersistentCache(unityPackedUrl)
            frameworkLogger.info("Unity was downloaded")
        }

        val args = mutableListOf("-logfile", logPath.toString(), "-silent-crashes", "-riderIntegrationTests", "-batchMode")
        args.add("-executeMethod")
        if (resetEditorPrefs) {
            args.add("Editor.IntegrationTestHelper.ResetAndStart")
        } else {
            args.add("Editor.IntegrationTestHelper.Start")
        }

        if (useRiderTestPath) {
            args.add("-riderTestPath")
        }

        if (isRunningInTeamCity) {
            val login = System.getenv("login")
            val password = System.getenv("password")
            assertNotNull(login, "System.getenv(\"login\") is null.")
            assertNotNull(password, "System.getenv(\"password\") is null.")
            args.addAll(arrayOf("-username", login, "-password", password))
        }

        frameworkLogger.info("Starting unity process")
        val process = StartUnityAction.startUnity(project, *args.toTypedArray())
        assertNotNull(process, "Unity process wasn't started")
        frameworkLogger.info("Unity process started: $process")

        return process
    }

    private fun killUnity(process: Process) {
        frameworkLogger.info("Trying to kill unity process")
        if (!process.isAlive) {
            frameworkLogger.info("Unity process isn't alive")
            return
        }
        process.destroy()
        waitAndPump(project.lifetime, { !process.isAlive }, defaultTimeout) { "Process should have existed." }
        frameworkLogger.info("Unity killed")
    }

    fun withUnityProcess(resetEditorPrefs: Boolean, useRiderTestPath: Boolean = false, block: () -> Unit) {
        val process = startUnity(resetEditorPrefs, useRiderTestPath)
        try {
            block()
        } finally {
            killUnity(process)
        }
    }

    fun installPlugin() {
        frameworkLogger.info("Trying to install editor plugin")
        rdUnityModel.installEditorPlugin.fire(Unit)

        val editorPluginPath = Paths.get(project.basePath!!)
            .resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")
        waitAndPump(project.lifetime, { editorPluginPath.exists() }, Duration.ofSeconds(10)) { "EditorPlugin was not installed." }
        frameworkLogger.info("Editor plugin was installed")
    }

    fun executeScript(file: String) {
        val script = solutionSourceRootDirectory.combine("scripts", file)
        script.copyTo(activeSolutionDirectory.combine("Assets", file))

        frameworkLogger.info("Executing script '$file'")
        refreshUnityModel()
    }

    fun refreshUnityModel() {
        frameworkLogger.info("Refreshing unity model")
        project.solution.rdUnityModel.refresh.fire(true)
    }

    fun executeMethod(runMethodData: RunMethodData): RunMethodResult {
        frameworkLogger.info("Executing method ${runMethodData.methodName} from ${runMethodData.typeName} (assembly: ${runMethodData.assemblyName})")
        val runMethodResult = rdUnityModel.runMethodInUnity.callSynchronously(runMethodData, project.protocol)!!
        assertTrue(runMethodResult.success, "runMethodResult.success is false \n${runMethodResult.message} \n${runMethodResult.stackTrace}")
        frameworkLogger.info("Method was executed")
        return runMethodResult
    }

    fun waitFirstScriptCompilation() {
        frameworkLogger.info("Waiting for .start file exist")
        val unityStartFile = Paths.get(project.basePath!!).resolve(".start")
        waitAndPump(project.lifetime, { unityStartFile.exists() }, defaultTimeout) { "Unity was not started." }
        frameworkLogger.info("Unity started (.start file exist)")
    }

    fun waitConnection() {
        frameworkLogger.info("Waiting for connection between Unity editor and Rider")
        waitAndPump(project.lifetime, { rdUnityModel.sessionInitialized.hasTrueValue },
            defaultTimeout) { "unityHost is not initialized." }
        frameworkLogger.info("unityHost is initialized.")
    }

    @AfterClass
    fun terminateLifetime() {
        lifetimeDefinition.terminate()
    }
}