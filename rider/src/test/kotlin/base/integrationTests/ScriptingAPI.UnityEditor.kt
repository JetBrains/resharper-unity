package base.integrationTests

import com.intellij.execution.RunManager
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.intellij.util.io.exists
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rdclient.util.idea.callSynchronously
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.model.EditorLogEntry
import com.jetbrains.rider.model.RunMethodData
import com.jetbrains.rider.model.RunMethodResult
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.protocol.protocol
import com.jetbrains.rider.services.popups.nova.headless.NullPrintStream
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import java.io.File
import java.io.PrintStream
import java.nio.file.Paths
import java.time.Duration
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

//region Connection

fun startUnity(project: Project, logPath: File, resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean): Process {
    val args = mutableListOf("-logfile", logPath.toString(), "-silent-crashes", "-riderIntegrationTests")
    if (batchMode) {
        args.add("-batchMode")
    }

    args.add("-executeMethod")
    if (resetEditorPrefs) {
        args.add("Editor.IntegrationTestHelper.ResetAndStart")
    } else {
        args.add("Editor.IntegrationTestHelper.Start")
    }

    if (useRiderTestPath) {
        args.add("-riderTestPath")
    }

    if (TeamCityHelper.isUnderTeamCity) {
        val login = System.getenv("unity.login")
        val password = System.getenv("unity.password")
        assertNotNull(login, "System.getenv(\"unity.login\") is null.")
        assertNotNull(password, "System.getenv(\"unity.password\") is null.")
        args.addAll(arrayOf("-username", login, "-password", password))
    }

    frameworkLogger.info("Starting unity process")
    val process = StartUnityAction.startUnity(project, *args.toTypedArray())
    assertNotNull(process, "Unity process wasn't started")
    frameworkLogger.info("Unity process started: $process")

    return process
}

fun IntegrationTestBase.startUnity(resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean) =
    startUnity(project, testMethod.logDirectory.resolve("UnityEditor.log"), resetEditorPrefs, useRiderTestPath, batchMode)

fun killUnity(project: Project, process: Process) {
    frameworkLogger.info("Trying to kill unity process")
    if (!process.isAlive) {
        frameworkLogger.info("Unity process isn't alive")
        return
    }
    process.destroy()
    waitAndPump(project.lifetime, { !process.isAlive }, IntegrationTestBase.defaultTimeout) { "Process should have existed." }
    frameworkLogger.info("Unity process killed")
}

fun IntegrationTestBase.killUnity(process: Process) = killUnity(project, process)

fun IntegrationTestBase.withUnityProcess(
    resetEditorPrefs: Boolean,
    useRiderTestPath: Boolean = false,
    batchMode: Boolean = true,
    block: Process.() -> Unit
) {
    val process = this.startUnity(resetEditorPrefs, useRiderTestPath, batchMode)
    try {
        process.block()
    } finally {
        this.killUnity(process)
    }
}

fun installPlugin(project: Project) {
    frameworkLogger.info("Trying to install editor plugin")
    project.solution.rdUnityModel.installEditorPlugin.fire(Unit)

    val editorPluginPath = Paths.get(project.basePath!!)
        .resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")
    waitAndPump(project.lifetime, { editorPluginPath.exists() }, Duration.ofSeconds(10)) { "EditorPlugin was not installed." }
    frameworkLogger.info("Editor plugin was installed")
}

fun IntegrationTestBase.installPlugin() = installPlugin(project)

fun IntegrationTestBase.executeScript(file: String) {
    val script = testCaseSourceDirectory.combine(file)
    script.copyTo(activeSolutionDirectory.combine("Assets", file))

    frameworkLogger.info("Executing script '$file'")
    refreshUnityModel()
}

fun IntegrationTestBase.refreshUnityModel() {
    frameworkLogger.info("Refreshing unity model")
    project.solution.rdUnityModel.refresh.fire(true)
}

private fun IntegrationTestBase.executeMethod(runMethodData: RunMethodData): RunMethodResult {
    frameworkLogger.info("Executing method ${runMethodData.methodName} from ${runMethodData.typeName} (assembly: ${runMethodData.assemblyName})")
    val runMethodResult = project.solution.rdUnityModel.runMethodInUnity.callSynchronously(runMethodData, project.protocol)!!
    assertTrue(runMethodResult.success, "runMethodResult.success is false \n${runMethodResult.message} \n${runMethodResult.stackTrace}")
    frameworkLogger.info("Method was executed")
    return runMethodResult
}

fun IntegrationTestBase.waitFirstScriptCompilation() {
    frameworkLogger.info("Waiting for .start file exist")
    val unityStartFile = Paths.get(project.basePath!!).resolve(".start")
    waitAndPump(project.lifetime, { unityStartFile.exists() }, IntegrationTestBase.defaultTimeout) { "Unity was not started." }
    frameworkLogger.info("Unity started (.start file exist)")
}

fun IntegrationTestBase.waitConnection() {
    frameworkLogger.info("Waiting for connection between Unity editor and Rider")
    waitAndPump(project.lifetime, { project.isConnectedToEditor() },
        IntegrationTestBase.defaultTimeout) { "unityHost is not initialized." }
    frameworkLogger.info("unityHost is initialized.")
}

fun IntegrationTestBase.checkSweaInSolution() {
    changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
    checkSwea(project)
}

fun IntegrationTestBase.executeIntegrationTestMethod(methodName: String) =
    executeMethod(RunMethodData("Assembly-CSharp-Editor", "Editor.IntegrationTestHelper", methodName))

fun printEditorLogEntry(stream: PrintStream, editorLogEntry: EditorLogEntry) {
    val type = RdLogEventType.values()[editorLogEntry.type]
    val mode = RdLogEventMode.values()[editorLogEntry.mode]
    if (type == RdLogEventType.Message) {
        stream.appendln("$type, $mode, ${editorLogEntry.message}\n " +
            editorLogEntry.stackTrace.replace(Regex(" \\(at .+\\)"), ""))
    }
}

//endregion

//region Playing


fun IntegrationTestBase.play() = waitForPlayModeAfterAction { rdUnityModel.play.set(true) }

fun IntegrationTestBase.pause() = waitForPauseModeAfterAction { rdUnityModel.pause.set(true) }

fun IntegrationTestBase.step() = waitForStepAfterAction { rdUnityModel.step.fire(Unit) }

fun IntegrationTestBase.stopPlaying() = waitForIdleModeAfterAction { rdUnityModel.play.set(false) }

fun IntegrationTestBase.unpause() = waitForPlayModeAfterAction { rdUnityModel.pause.set(false) }

fun IntegrationTestBase.waitForPlayModeAfterAction(action: () -> Unit) =
    waitForEditorLogAfterAction("Play", "Unity editor isn't in 'Play' mode", action)

fun IntegrationTestBase.waitForIdleModeAfterAction(action: () -> Unit) =
    waitForEditorLogAfterAction("Idle", "Unity editor isn't in 'Idle' mode", action)

fun IntegrationTestBase.waitForPauseModeAfterAction(action: () -> Unit) =
    waitForEditorLogAfterAction("Pause", "Unity editor isn't in 'Pause' mode", action)

fun IntegrationTestBase.waitForStepAfterAction(action: () -> Unit) =
    waitForEditorLogAfterAction("Step", "Unity editor didn't make step", action)

private fun IntegrationTestBase.waitForEditorLogAfterAction(logMessage: String, failMessage: String, action: () -> Unit) {
    val logLifetime = lifetime.createNested()
    rdUnityModel.onUnityLogEvent.adviseNotNull(logLifetime) {
        if (it.message == logMessage) {
            logLifetime.terminate()
        }
    }
    action()
    if (logMessage != "Step") pumpMessages(Duration.ofSeconds(5)) // TODO: remove after fix logs
    else waitAndPump(Duration.ofSeconds(15), { logLifetime.isNotAlive }) { failMessage }
}

fun IntegrationTestBase.restart() {
    stopPlaying()
    play()
}

//endregion

//region Debug

private fun IntegrationTestBase.selectRunConfiguration(name: String) {
    val runManager = RunManager.getInstance(project)
    val runConfigurationToSelect = runManager.allConfigurationsList.firstOrNull {
        it.name == name
    }.shouldNotBeNull("There are no run configuration with name '$name', " +
        "current: ${runManager.allConfigurationsList.joinToString(", ", "[", "]")}")

    frameworkLogger.info("Selecting run configuration '$name'")
    runManager.selectedConfiguration = runManager.findSettings(runConfigurationToSelect)
}

fun IntegrationTestBase.attachDebuggerToUnityEditorAndPlay(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) = attachDebuggerToUnityEditor(true, beforeRun, test, goldFile)

fun IntegrationTestBase.attachDebuggerToUnityEditor(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) = attachDebuggerToUnityEditor(false, beforeRun, test, goldFile)

private fun IntegrationTestBase.attachDebuggerToUnityEditor(
    andPlay: Boolean,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) {
    selectRunConfiguration(
        if (andPlay) DefaultRunConfigurationGenerator.ATTACH_AND_PLAY_CONFIGURATION_NAME
        else DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME
    )

    val lifetimeDef = lifetime.createNested()
    val subscribeAndBeforeRun = if (andPlay) ({
        rdUnityModel.play.adviseNotNull(lifetimeDef) {
            if (it) lifetimeDef.terminate()
        }
        beforeRun()
    }) else beforeRun
    val waitAndTest = if (andPlay) ({
        waitAndPump(Duration.ofSeconds(20), { lifetimeDef.isNotAlive })
        { "Unity editor not in 'Play' mode after run configuration" }
        test()
    }) else test

    if (goldFile != null) {
        debugUnityProgramWithGold(goldFile, subscribeAndBeforeRun, waitAndTest)
    } else {
        debugUnityProgramWithoutGold(subscribeAndBeforeRun, waitAndTest)
    }
}

private fun IntegrationTestBase.debugUnityProgramWithGold(goldFile: File, beforeRun: ExecutionEnvironment.() -> Unit = {}, test: DebugTestExecutionContext.() -> Unit) =
    testDebugProgram(goldFile, beforeRun, test, {}, true)

private fun IntegrationTestBase.debugUnityProgramWithoutGold(beforeRun: ExecutionEnvironment.() -> Unit = {}, test: DebugTestExecutionContext.() -> Unit) =
    debugProgram(NullPrintStream, beforeRun, test, {}, true)

//endregion