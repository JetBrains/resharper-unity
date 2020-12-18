package base.integrationTests

import com.intellij.execution.RunManager
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.intellij.util.text.VersionComparatorUtil
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
import com.jetbrains.rider.model.unity.*
import com.jetbrains.rider.model.unity.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.model.unity.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToPausepoint
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.withProjectPath
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

fun replaceUnityVersionOnCurrent(project: Project) {
    val projectVersionFile = File(project.basePath, "ProjectSettings").resolve("ProjectVersion.txt")
    val oldVersion = projectVersionFile.readText().split(Regex("\\s+"))[1]

    val newVersion = UnityInstallationFinder.getInstance(project).getApplicationVersion()

    frameworkLogger.info("Replace unity project version '$oldVersion' by '$newVersion'")
    projectVersionFile.writeText("m_EditorVersion: $newVersion")
}

fun IntegrationTestWithFrontendBackendModel.activateRiderFrontendTest() {
    frameworkLogger.info("Set frontendBackendModel.riderFrontendTests = true")
    if (!frontendBackendModel.riderFrontendTests.valueOrDefault(false)) {
        frontendBackendModel.riderFrontendTests.set(true)
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
    val args = getUnityArgs(project).withProjectPath(project)
    args.addAll(arrayOf("-logfile", logPath.toString(), "-silent-crashes", "-riderIntegrationTests"))
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

    val relPath = when {
        SystemInfo.isWindows -> "net461/rider-dev.app/rider-dev.bat"
        SystemInfo.isMac -> "net461/rider-dev.app"
        else -> throw Exception("Not implemented")
    }

    val riderPath = Paths.get(UnityTestEnvironment::class.java.getProtectionDomain().getCodeSource().getLocation().toURI())
        .parent.parent.parent.parent.parent.resolve("unity/build/EditorPluginNet46/bin").toFile().listFiles()
        .filter { a-> (a.name=="Debug"|| a.name=="Release") && a.exists() && a.isDirectory }.single().toPath().resolve(relPath)
        .toString()
    args.addAll(arrayOf("-riderPath", riderPath))

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
//            val unityProjectDefaultArgsString = getUnityWithProjectArgs(project)
//                .drop(1)
//                .toMutableList()
//                .apply { addAll(args) }
//                .let {
//                    when {
//                        SystemInfo.isWindows -> it.joinToString(" ")
//                        else -> ParametersList.join(it)
//                    }
//                }
//            val unityInstallationFinder = UnityInstallationFinder.getInstance(project)
//            val unityConfigurationParameters = RdDotCoverUnityConfigurationParameters(
//                unityInstallationFinder.getApplicationExecutablePath().toString(),
//                unityProjectDefaultArgsString,
//                unityInstallationFinder.getApplicationVersion()
//            )
//
//            project.solution.dotCoverModel.fire(unityConfigurationParameters)
//            getUnityProcessHandle(project)
            throw NotImplementedError()
        }
        else -> StartUnityAction.startUnity(args)?.toHandle()
    }
    assertNotNull(processHandle, "Unity process wasn't started")
    frameworkLogger.info("Unity process started [pid: ${processHandle.pid()}]")

    return processHandle
}

fun getUnityProcessHandle(project: Project): ProcessHandle {
    val unityApplicationData = project.solution.frontendBackendModel.unityApplicationData
    waitAndPump(unityDefaultTimeout, { unityApplicationData.valueOrNull?.unityProcessId != null }) { "Can't get unity process id" }
    return ProcessHandle.of(unityApplicationData.valueOrNull?.unityProcessId!!.toLong()).get()
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

    // clean up Unity Preferences https://docs.unity3d.com/ScriptReference/EditorPrefs.html#:~:text=Stores%20and%20accesses%20Unity%20editor,unity3d.
    if (SystemInfo.isMac) // remove ~/Library/Preferences/com.unity3d.UnityEditor5.x.plist
    {
        val home = System.getProperty("user.home")
        // not really needed right now - need to find a better way
        // Paths.get(home).resolve("Library/Preferences/com.unity3d.UnityEditor5.x.plist").toFile().deleteRecursively()
    }
}

fun killUnity(project: Project) = killUnity(project, getUnityProcessHandle(project))

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
    val unityVersion: String? = UnityInstallationFinder.getInstance(project).getApplicationVersion(2)
    if (unityVersion != null && VersionComparatorUtil.compare(unityVersion, "2019.2") >= 0) {
        frameworkLogger.info("Unity version $unityVersion, no need to install EditorPlugin.")
        return
    }
    frameworkLogger.info("Trying to install editor plugin")
    project.solution.frontendBackendModel.installEditorPlugin.fire(Unit)

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
    project.solution.frontendBackendModel.refreshUnityModel()
}

fun FrontendBackendModel.refreshUnityModel() {
    frameworkLogger.info("Refreshing unity model")
    refresh.fire(true)
}

fun IntegrationTestWithFrontendBackendModel.refreshUnityModel() = frontendBackendModel.refreshUnityModel()

private fun IntegrationTestWithFrontendBackendModel.executeMethod(runMethodData: RunMethodData): RunMethodResult {
    frameworkLogger.info("Executing method ${runMethodData.methodName} from ${runMethodData.typeName} (assembly: ${runMethodData.assemblyName})")
    val runMethodResult = frontendBackendModel.runMethodInUnity.callSynchronously(runMethodData, frontendBackendModel.protocol)!!
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
                && project.solution.frontendBackendModel.unityEditorState.valueOrDefault(UnityEditorState.Disconnected) != UnityEditorState.Disconnected
        },
        unityDefaultTimeout) { "unityHost is not initialized." }
    frameworkLogger.info("unityHost is initialized.")
}

fun BaseTestWithSolutionBase.checkSweaInSolution(project: Project) {
    changeFileSystem2(project) { arrayOf(File(project.basePath, "Assembly-CSharp.csproj")) }
    checkSwea(project)
}

fun BaseTestWithSolution.checkSweaInSolution() = checkSweaInSolution(project)

fun IntegrationTestWithFrontendBackendModel.executeIntegrationTestMethod(methodName: String) =
    executeMethod(RunMethodData("Assembly-CSharp-Editor", "Editor.IntegrationTestHelper", methodName))

fun printEditorLogEntry(stream: PrintStream, logEvent: LogEvent) {
    if (logEvent.type == LogEventType.Message) {
        stream.println("${logEvent.type}, ${logEvent.mode}, ${logEvent.message}\n " +
            logEvent.stackTrace.replace(Regex(" \\(at .+\\)"), ""))
    }
}

//endregion

//region Playing

fun IntegrationTestWithFrontendBackendModel.play(waitForPlay: Boolean = true) {
    frameworkLogger.info("Start playing in unity editor")
    frontendBackendModel.playControls.play.set(true)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun IntegrationTestWithFrontendBackendModel.pause(waitForPause: Boolean = true) {
    frameworkLogger.info("Pause unity editor")
    frontendBackendModel.playControls.pause.set(true)
    if (waitForPause) waitForUnityEditorPauseMode()
}

// "2000000" is default log message in NewBehaviourScript.Update() in test solutions
fun IntegrationTestWithFrontendBackendModel.step(waitForStep: Boolean = true, logMessageAfterStep: String = "2000000") {
    frameworkLogger.info("Make step in unity editor")
    if (waitForStep) {
        waitForEditorLogsAfterAction(logMessageAfterStep) { frontendBackendModel.playControls.step.fire(Unit) }
    } else {
        frontendBackendModel.playControls.step.fire(Unit)
    }
}

fun IntegrationTestWithFrontendBackendModel.stopPlaying(waitForIdle: Boolean = true) {
    frameworkLogger.info("Stop playing in unity editor")
    frontendBackendModel.playControls.play.set(false)
    if (waitForIdle) waitForUnityEditorIdleMode()
}

fun IntegrationTestWithFrontendBackendModel.unpause(waitForPlay: Boolean = true) {
    frameworkLogger.info("Unpause unity editor")
    frontendBackendModel.playControls.pause.set(false)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun IntegrationTestWithFrontendBackendModel.waitForUnityEditorPlayMode() = waitForUnityEditorState(UnityEditorState.Play)

fun IntegrationTestWithFrontendBackendModel.waitForUnityEditorPauseMode() = waitForUnityEditorState(UnityEditorState.Pause)

fun IntegrationTestWithFrontendBackendModel.waitForUnityEditorIdleMode() = waitForUnityEditorState(UnityEditorState.Idle)

fun IntegrationTestWithFrontendBackendModel.waitForEditorLogsAfterAction(vararg expectedMessages: String, action: () -> Unit): List<LogEvent> {
    val logLifetime = Lifetime.Eternal.createNested()
    val setOfMessages = expectedMessages.toHashSet()
    val editorLogEntries = mutableListOf<LogEvent>()
    frontendBackendModel.consoleLogging.onConsoleLogEvent.adviseNotNull(logLifetime) {
        if (setOfMessages.remove(it.message)) {
            editorLogEntries.add(it)
        }
        if (setOfMessages.isEmpty()) {
            logLifetime.terminate()
        }
    }
    val messagesString = setOfMessages.joinToString(", ", "[", "]")
    action()
    frameworkLogger.info("Waiting for log entries with messages: $messagesString")
    waitAndPump(unityActionsTimeout, { logLifetime.isNotAlive })
    { "Missed logs: ${setOfMessages.joinToString(", ", "[", "]")}" }
    return editorLogEntries
}

private fun IntegrationTestWithFrontendBackendModel.waitForUnityEditorState(editorState: UnityEditorState) {
    frameworkLogger.info("Waiting for unity editor in state '$editorState'")
    waitAndPump(unityActionsTimeout, { frontendBackendModel.unityEditorState.valueOrNull == editorState })
    { "Unity editor isn't in state '$editorState', actual state '${frontendBackendModel.unityEditorState.valueOrNull}'" }
}

fun IntegrationTestWithFrontendBackendModel.restart() {
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

fun IntegrationTestWithFrontendBackendModel.preferStandaloneNUnitLauncherInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.NUnit)

fun IntegrationTestWithFrontendBackendModel.preferEditModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.EditMode)

fun IntegrationTestWithFrontendBackendModel.preferPlayModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.PlayMode)

private fun IntegrationTestWithFrontendBackendModel.selectUnitTestLaunchPreference(preference: UnitTestLaunchPreference) {
    frameworkLogger.info("Selecting unit test launch preference '$preference'")
    frontendBackendModel.unitTestPreference.set(preference)
}

//endregion

//region Interface

//Needed to use extensions in all base classes
interface IntegrationTestWithFrontendBackendModel {
    val frontendBackendModel: FrontendBackendModel
}

//endregion
