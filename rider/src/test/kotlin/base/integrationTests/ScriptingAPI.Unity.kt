package base.integrationTests

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ParametersList
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.callSynchronously
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.model.*
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToPausepoint
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.getUnityWithProjectArgs
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.services.popups.nova.headless.NullPrintStream
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.framework.TeamCityHelper
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.*
import java.io.File
import java.io.PrintStream
import java.nio.file.Files
import java.nio.file.Paths
import java.time.Duration
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

//region Timeouts

val unityDefaultTimeout: Duration = Duration.ofSeconds(120)

val unityActionsTimeout: Duration = Duration.ofSeconds(30)

//endregion

//region UnityDll

fun downloadUnityDll(): File {
    return downloadAndExtractArchiveArtifactIntoPersistentCache("https://repo.labs.intellij.net/dotnet-rider-test-data/UnityEngine-2018.3-08-01-2019.dll.zip").combine("UnityEngine.dll")
}

fun copyUnityDll(unityDll: File, project: Project, activeSolutionDirectory: File) {
    copyUnityDll(unityDll, activeSolutionDirectory)
    refreshFileSystem(project)
}

fun copyUnityDll(unityDll: File, activeSolutionDirectory: File) {
    unityDll.copyTo(activeSolutionDirectory.combine("UnityEngine.dll"))
}

//endregion

//region Connection

fun createLibraryFolderIfNotExist(solutionDirectory: File) {
    // Needed, because com.jetbrains.rider.plugins.unity.ProtocolInstanceWatcher
    //  isn't initialized without correct unity file structure
    val libraryFolder = Paths.get(solutionDirectory.toString(), "Library")
    if (!libraryFolder.exists()) {
        Files.createDirectory(libraryFolder)
    }
}

fun IntegrationTestWithRdUnityModel.activateRiderFrontendTest() {
    frameworkLogger.info("Set rdUnityModel.riderFrontendTests = true")
    if (!rdUnityModel.riderFrontendTests.valueOrDefault(false)) {
        rdUnityModel.riderFrontendTests.set(true)
    }
}

fun allowUnityPathVfsRootAccess(lifetimeDefinition: LifetimeDefinition) {
    val unityPath = when {
        SystemInfo.isWindows -> "C:/Program Files/Unity"
        SystemInfo.isMac -> "/Applications/Unity"
        else -> throw Exception("Not implemented")
    }
    VfsRootAccess.allowRootAccess(lifetimeDefinition.createNestedDisposable("Unity path disposable"), unityPath)
}

fun startUnity(project: Project, logPath: File, withCoverage: Boolean, resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean): ProcessHandle {
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

    frameworkLogger.info("Starting unity process${if (withCoverage) " with Coverage" else ""}")
    val processHandle = when {
        withCoverage -> {
            val unityProjectDefaultArgsString = getUnityWithProjectArgs(project)
                .drop(1)
                .toMutableList()
                .apply { addAll(args) }
                .let {
                    when {
                        SystemInfo.isWindows -> it.joinToString(" ")
                        else -> ParametersList.join(it)
                    }
                }
            val unityInstallationFinder = UnityInstallationFinder.getInstance(project)
            val unityConfigurationParameters = RdDotCoverUnityConfigurationParameters(
                unityInstallationFinder.getApplicationExecutablePath().toString(),
                unityProjectDefaultArgsString,
                unityInstallationFinder.getApplicationVersion()
            )
            project.solution.dotCoverModel.unityCoverageRequested.fire(unityConfigurationParameters)
            val unityProcessId = project.solution.rdUnityModel.unityProcessId
            waitAndPump(unityDefaultTimeout, { unityProcessId.valueOrNull != null }) { "Can't get unity process id" }
            ProcessHandle.of(unityProcessId.valueOrNull!!.toLong()).get()
        }
        else -> StartUnityAction.startUnity(project, *args.toTypedArray())?.toHandle()
    }
    assertNotNull(processHandle, "Unity process wasn't started")
    frameworkLogger.info("Unity process started [pid: ${processHandle.pid()}]")

    return processHandle
}

fun BaseTestWithSolutionBase.startUnity(project: Project, withCoverage: Boolean, resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean) =
    startUnity(project, testMethod.logDirectory.resolve("UnityEditor.log"), withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)

fun BaseTestWithSolution.startUnity(withCoverage: Boolean, resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean) =
    startUnity(project, withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)

fun killUnity(project: Project, processHandle: ProcessHandle) {
    frameworkLogger.info("Trying to kill unity process")
    if (!processHandle.isAlive) {
        frameworkLogger.info("Unity process isn't alive")
        return
    }
    processHandle.destroy()
    waitAndPump(project.lifetime, { !processHandle.isAlive }, unityDefaultTimeout) { "Process should have existed." }
    frameworkLogger.info("Unity process killed")
}

fun BaseTestWithSolution.killUnity(processHandle: ProcessHandle) = killUnity(project, processHandle)

fun BaseTestWithSolution.withUnityProcess(
    withCoverage: Boolean = false,
    resetEditorPrefs: Boolean = false,
    useRiderTestPath: Boolean = false,
    batchMode: Boolean = true,
    block: ProcessHandle.() -> Unit
) {
    val processHandle = startUnity(withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)
    try {
        processHandle.block()
    } finally {
        killUnity(project, processHandle)
    }
}

fun installPlugin(project: Project) {
    frameworkLogger.info("Trying to install editor plugin")
    project.solution.rdUnityModel.installEditorPlugin.fire(Unit)

    val editorPluginPath = Paths.get(project.basePath!!)
        .resolve("Assets/Plugins/Editor/JetBrains/JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll")
    waitAndPump(project.lifetime, { editorPluginPath.exists() }, unityActionsTimeout) { "EditorPlugin was not installed." }
    frameworkLogger.info("Editor plugin was installed")
}

fun BaseTestWithSolution.installPlugin() = installPlugin(project)

fun BaseTestWithSolution.executeScript(file: String) {
    val script = testCaseSourceDirectory.combine(file)
    script.copyTo(activeSolutionDirectory.combine("Assets", file))

    frameworkLogger.info("Executing script '$file'")
    project.solution.rdUnityModel.refreshUnityModel()
}

fun RdUnityModel.refreshUnityModel() {
    frameworkLogger.info("Refreshing unity model")
    refresh.fire(true)
}

fun IntegrationTestWithRdUnityModel.refreshUnityModel() = rdUnityModel.refreshUnityModel()

private fun IntegrationTestWithRdUnityModel.executeMethod(runMethodData: RunMethodData): RunMethodResult {
    frameworkLogger.info("Executing method ${runMethodData.methodName} from ${runMethodData.typeName} (assembly: ${runMethodData.assemblyName})")
    val runMethodResult = rdUnityModel.runMethodInUnity.callSynchronously(runMethodData, rdUnityModel.protocol)!!
    assertTrue(runMethodResult.success, "runMethodResult.success is false \n${runMethodResult.message} \n${runMethodResult.stackTrace}")
    frameworkLogger.info("Method was executed")
    return runMethodResult
}

fun waitFirstScriptCompilation(project: Project) {
    frameworkLogger.info("Waiting for .start file exist")
    val unityStartFile = Paths.get(project.basePath!!).resolve(".start")
    waitAndPump(project.lifetime, { unityStartFile.exists() }, unityDefaultTimeout) { "Unity was not started." }
    frameworkLogger.info("Unity started (.start file exist)")
}

fun waitConnectionToUnityEditor(project: Project) {
    frameworkLogger.info("Waiting for connection between Unity editor and Rider")
    waitAndPump(project.lifetime,
        {
            project.isConnectedToEditor()
                && project.solution.rdUnityModel.editorState.valueOrDefault(EditorState.Disconnected) != EditorState.Disconnected
        },
        unityDefaultTimeout) { "unityHost is not initialized." }
    frameworkLogger.info("unityHost is initialized.")
}

fun BaseTestWithSolutionBase.checkSweaInSolution(project: Project) {
    changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
    checkSwea(project)
}

fun BaseTestWithSolution.checkSweaInSolution() = checkSweaInSolution(project)

fun IntegrationTestWithRdUnityModel.executeIntegrationTestMethod(methodName: String) =
    executeMethod(RunMethodData("Assembly-CSharp-Editor", "Editor.IntegrationTestHelper", methodName))

fun printEditorLogEntry(stream: PrintStream, editorLogEntry: EditorLogEntry) {
    val type = RdLogEventType.values()[editorLogEntry.type]
    val mode = RdLogEventMode.values()[editorLogEntry.mode]
    if (type == RdLogEventType.Message) {
        stream.println("$type, $mode, ${editorLogEntry.message}\n " +
            editorLogEntry.stackTrace.replace(Regex(" \\(at .+\\)"), ""))
    }
}

//endregion

//region Playing

fun IntegrationTestWithRdUnityModel.play(waitForPlay: Boolean = true) {
    frameworkLogger.info("Start playing in unity editor")
    rdUnityModel.play.set(true)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun IntegrationTestWithRdUnityModel.pause(waitForPause: Boolean = true) {
    frameworkLogger.info("Pause unity editor")
    rdUnityModel.pause.set(true)
    if (waitForPause) waitForUnityEditorPauseMode()
}

// "2000000" is default log message in NewBehaviourScript.Update() in test solutions
fun IntegrationTestWithRdUnityModel.step(logMessageAfterStep: String = "2000000") =
    frameworkLogger.info("Make step in unity editor")
    waitForEditorLogsAfterAction(logMessageAfterStep) { rdUnityModel.step.fire(Unit) }
}

fun IntegrationTestWithRdUnityModel.stopPlaying(waitForIdle: Boolean = true) {
    frameworkLogger.info("Stop playing in unity editor")
    rdUnityModel.play.set(false)
    if (waitForIdle) waitForUnityEditorIdleMode()
}

fun IntegrationTestWithRdUnityModel.unpause(waitForPlay: Boolean = true) {
    frameworkLogger.info("Unpause unity editor")
    rdUnityModel.pause.set(false)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun IntegrationTestWithRdUnityModel.waitForUnityEditorPlayMode() = waitForUnityEditorState(EditorState.ConnectedPlay)

fun IntegrationTestWithRdUnityModel.waitForUnityEditorPauseMode() = waitForUnityEditorState(EditorState.ConnectedPause)

fun IntegrationTestWithRdUnityModel.waitForUnityEditorIdleMode() = waitForUnityEditorState(EditorState.ConnectedIdle)

fun IntegrationTestWithRdUnityModel.waitForEditorLogAfterAction(logMessage: String, action: () -> Unit): EditorLogEntry {
    val logLifetime = Lifetime.Eternal.createNested()
    var editorLogEntry: EditorLogEntry? = null
    rdUnityModel.onUnityLogEvent.adviseNotNull(logLifetime) {
        if (it.message == logMessage) {
            editorLogEntry = it
            logLifetime.terminate()
        }
    }
    action()
    frameworkLogger.info("Waiting for log entry with message: $logMessage")
    waitAndPump(unityActionsTimeout, { logLifetime.isNotAlive })
    { "There are no log entry with message: $logMessage" }
    return editorLogEntry!!
}

private fun IntegrationTestWithRdUnityModel.waitForUnityEditorState(editorState: EditorState) {
    frameworkLogger.info("Waiting for unity editor in state '$editorState'")
    waitAndPump(unityActionsTimeout, { rdUnityModel.editorState.valueOrNull == editorState })
    { "Unity editor isn't in state '$editorState', actual state '${rdUnityModel.editorState.valueOrNull}'" }
}

fun IntegrationTestWithRdUnityModel.restart() {
    stopPlaying()
    play()
}

//endregion

//region RunDebug

fun waitForUnityRunConfigurations(project: Project) {
    val runManager = RunManager.getInstance(project)
    waitAndPump(unityActionsTimeout, { runManager.allConfigurationsList.size >= 2 }) {
        "Unity run configurations didn't appeared, " +
            "current: ${runManager.allConfigurationsList.joinToString(", ", "[", "]")}"
    }
}

private fun selectRunConfiguration(project: Project, name: String) {
    val runManager = RunManager.getInstance(project)
    val runConfigurationToSelect = runManager.allConfigurationsList.firstOrNull {
        it.name == name
    }.shouldNotBeNull("There are no run configuration with name '$name', " +
        "current: ${runManager.allConfigurationsList.joinToString(", ", "[", "]")}")

    frameworkLogger.info("Selecting run configuration '$name'")
    runManager.selectedConfiguration = runManager.findSettings(runConfigurationToSelect)
}

fun attachDebuggerToUnityEditorAndPlay(
    project: Project,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) = attachDebuggerToUnityEditor(project, true, beforeRun, test, goldFile)

fun BaseTestWithSolution.attachDebuggerToUnityEditorAndPlay(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null) = attachDebuggerToUnityEditorAndPlay(project, beforeRun, test, goldFile)

fun attachDebuggerToUnityEditor(
    project: Project,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) = attachDebuggerToUnityEditor(project, false, beforeRun, test, goldFile)

fun BaseTestWithSolution.attachDebuggerToUnityEditor(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null) = attachDebuggerToUnityEditor(project, beforeRun, test, goldFile)

private fun attachDebuggerToUnityEditor(
    project: Project,
    andPlay: Boolean,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) {
    selectRunConfiguration(
        project,
        if (andPlay) DefaultRunConfigurationGenerator.ATTACH_AND_PLAY_CONFIGURATION_NAME
        else DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME
    )

    val waitAndTest: DebugTestExecutionContext.() -> Unit = {
        waitForDotNetDebuggerInitializedOrCanceled()
        test()
    }

    if (goldFile != null) {
        debugUnityProgramWithGold(project, goldFile, beforeRun, waitAndTest)
    } else {
        debugUnityProgramWithoutGold(project, beforeRun, waitAndTest)
    }
}

private fun debugUnityProgramWithGold(project: Project, goldFile: File, beforeRun: ExecutionEnvironment.() -> Unit = {}, test: DebugTestExecutionContext.() -> Unit) =
    testDebugProgram(project, goldFile, beforeRun, test, {}, true)

private fun debugUnityProgramWithoutGold(project: Project, beforeRun: ExecutionEnvironment.() -> Unit = {}, test: DebugTestExecutionContext.() -> Unit) =
    debugProgram(project, NullPrintStream, beforeRun, test, {}, true)

fun BaseTestWithSolutionBase.toggleUnityPausepoint(project: Project, projectFile: String, lineNumber: Int, condition: String = ""): XLineBreakpoint<DotNetLineBreakpointProperties> {
    @Suppress("UNCHECKED_CAST")
    val breakpoint = toggleBreakpoint(project, projectFile, lineNumber)
        as XLineBreakpoint<DotNetLineBreakpointProperties>

    val unityPausepointType = XDebuggerUtil.getInstance()
        .findBreakpointType(UnityPausepointBreakpointType::class.java)
    val breakpointManager = XDebuggerManager.getInstance(project).breakpointManager

    val oldPausepointsCount = breakpointManager.getBreakpoints(unityPausepointType).size
    convertToPausepoint(project, breakpoint)
    frameworkLogger.info("Convert line breakpoint to unity pausepoint")

    waitAndPump(unityDefaultTimeout, { breakpointManager.getBreakpoints(unityPausepointType).size == oldPausepointsCount + 1 })
    { "Pausepoint isn't created" }
    val pausepoint = breakpointManager.getBreakpoints(unityPausepointType).first()
    pausepoint.condition = condition
    frameworkLogger.info("Set pausepoint condition: '$condition'")

    return pausepoint
}

//endregion

//region UnitTesting

fun IntegrationTestWithRdUnityModel.preferStandaloneNUnitLauncherInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.NUnit)

fun IntegrationTestWithRdUnityModel.preferEditModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.EditMode)

fun IntegrationTestWithRdUnityModel.preferPlayModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.PlayMode)

private fun IntegrationTestWithRdUnityModel.selectUnitTestLaunchPreference(preference: UnitTestLaunchPreference) {
    frameworkLogger.info("Selecting unit test launch preference '$preference'")
    rdUnityModel.unitTestPreference.set(preference)
}

//endregion

//region Interface

//Needed to use extensions in all base classes
interface IntegrationTestWithRdUnityModel {
    val rdUnityModel: RdUnityModel
}

//endregion