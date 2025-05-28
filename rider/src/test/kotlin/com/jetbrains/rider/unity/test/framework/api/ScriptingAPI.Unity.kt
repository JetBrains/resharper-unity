package com.jetbrains.rider.unity.test.framework.api

import com.intellij.execution.RunManager
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.components.ComponentManagerEx
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.util.SystemInfo
import com.intellij.util.WaitFor
import com.intellij.xdebugger.XDebugSession
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.XDebuggerUtil
import com.intellij.xdebugger.breakpoints.XLineBreakpoint
import com.jetbrains.rd.framework.protocolOrThrow
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isNotAlive
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.testFramework.getGoldFile
import com.jetbrains.rdclient.util.idea.callSynchronously
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.debugger.breakpoint.DotNetLineBreakpointProperties
import com.jetbrains.rider.plugins.unity.UnityPluginEnvironment
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.UnityPausepointBreakpointType
import com.jetbrains.rider.plugins.unity.debugger.breakpoints.convertToPausepoint
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.model.*
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityProcess
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.withProjectPath
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.solutionName
import com.jetbrains.rider.test.asserts.shouldNotBeNull
import com.jetbrains.rider.test.env.packages.ZipFilePackagePreparer
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.*
import com.jetbrains.rider.test.framework.processor.TestProcessor
import com.jetbrains.rider.test.framework.testData.TestDataStorage
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.cases.integrationTests.UnityPlayerDebuggerTestBase
import com.jetbrains.rider.utils.NullPrintStream
import kotlinx.coroutines.launch
import java.io.File
import java.io.PrintStream
import java.time.Duration
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

//region Timeouts

val unityDefaultTimeout: Duration = Duration.ofSeconds(120)

val unityActionsTimeout: Duration = Duration.ofSeconds(30)

//endregion

//region UnityDll

val unity2022_2_15f1_ref_asm by ZipFilePackagePreparer("Unity3d-2022.2.15f1-06-09-2023.zip")

fun prepareAssemblies(project: Project, activeSolutionDirectory: File) {
    prepareAssemblies(activeSolutionDirectory)
    refreshFileSystem(project)
}

fun prepareAssemblies(activeSolutionDirectory: File) {
    //moving all UnityEngine* and UnityEditor*, netstandard and mscorlib ref-asm dlls to test solution folder
    for (file in unity2022_2_15f1_ref_asm.root.listFiles()!!) {
        val target = activeSolutionDirectory.combine(file.name)
        file.copyTo(target)
    }
}

//endregion

//region Connection

fun replaceUnityVersionOnCurrent(project: Project) {
    val projectVersionFile = project.solutionDirectory.resolve("ProjectSettings/ProjectVersion.txt")
    val oldVersion = projectVersionFile.readText().split(Regex("\\s+"))[1]

    val newVersion = UnityInstallationFinder.getInstance(project).getApplicationVersion()

    frameworkLogger.info("Replace unity project version '$oldVersion' by '$newVersion'")
    projectVersionFile.writeText("m_EditorVersion: $newVersion")
}

fun SolutionApiFacade.activateRiderFrontendTest() {
    frameworkLogger.info("Set frontendBackendModel.riderFrontendTests = true")
    if (!frontendBackendModel.riderFrontendTests.valueOrDefault(false)) {
        frontendBackendModel.riderFrontendTests.set(true)
    }
}

fun startUnity(project: Project,
               logPath: File,
               withCoverage: Boolean,
               resetEditorPrefs: Boolean,
               useRiderTestPath: Boolean,
               batchMode: Boolean): ProcessHandle {
    val args = getUnityArgs(project).withProjectPath(project)
    return startUnity(args, logPath, withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)
}

fun startUnity(args: MutableList<String>,
                       logPath: File,
                       withCoverage: Boolean,
                       resetEditorPrefs: Boolean,
                       useRiderTestPath: Boolean,
                       batchMode: Boolean,
                       generateSolution: Boolean = false): ProcessHandle {
    val unityArgs = addArgsForUnityProcess(logPath, resetEditorPrefs, useRiderTestPath, batchMode, generateSolution)
    args.addAll(unityArgs)

    val riderPath = getRiderDevAppPath().canonicalPath
    args.addAll(arrayOf("-riderPath", riderPath))

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

fun getRiderDevAppPath(): File {
    val editorPluginFolderPath = UnityPluginEnvironment.getBundledFile("EditorPlugin")
    val assemblyName = "JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll"

    val riderDevAppPath = editorPluginFolderPath.resolve("rider-dev.app").apply { mkdirs() }
    val riderDevBatPath = riderDevAppPath.resolve("rider-dev.bat")

    val file = editorPluginFolderPath.resolve(assemblyName)
    if (!file.exists())
        throw IllegalStateException("editor plugin $file doesn't exist")

    riderDevBatPath.writeText(file.canonicalPath)

    return if (SystemInfo.isMac) riderDevAppPath else riderDevBatPath
}

fun TestProcessor<*>.startUnity(project: Project,
                                        withCoverage: Boolean,
                                        resetEditorPrefs: Boolean,
                                        useRiderTestPath: Boolean,
                                        batchMode: Boolean) =
    startUnity(project, testMethod.logDirectory.resolve("UnityEditor.log"), withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)

fun TestProcessor<*>.startUnity(executable: String,
                                        projectPath: String,
                                        withCoverage: Boolean,
                                        resetEditorPrefs: Boolean,
                                        useRiderTestPath: Boolean,
                                        batchMode: Boolean,
                                        generateSolution: Boolean = false): ProcessHandle {
    val args = mutableListOf(executable).withProjectPath(projectPath)
    return startUnity(args, testMethod.logDirectory.resolve("UnityEditor.log"), withCoverage, resetEditorPrefs, useRiderTestPath, batchMode,
                      generateSolution)
}

context(SolutionApiFacade, TestProcessor<*>)
fun startUnity(withCoverage: Boolean, resetEditorPrefs: Boolean, useRiderTestPath: Boolean, batchMode: Boolean) =
    startUnity(project, withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)

fun killUnity(processHandle: ProcessHandle) {
    frameworkLogger.info("Trying to kill unity process")
    if (!processHandle.isAlive) {
        frameworkLogger.info("Unity process isn't alive")
        return
    }
    processHandle.destroy()

    object : WaitFor(unityDefaultTimeout.toMillis().toInt(), 10000) {
        override fun condition(): Boolean {
            return !processHandle.isAlive
        }
    }.assertCompleted("Unity process was not killed")

    frameworkLogger.info("Unity process killed")
}

fun killUnity(project: Project) = killUnity(getUnityProcessHandle(project))

context(SolutionApiFacade, TestProcessor<*>)
fun withUnityProcess(
    withCoverage: Boolean = false,
    resetEditorPrefs: Boolean = false,
    useRiderTestPath: Boolean = false,
    batchMode: Boolean = true,
    block: ProcessHandle.() -> Unit
) {
    val processHandle = startUnity(withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)
    try {
        processHandle.block()
    }
    finally {
        killUnity(processHandle)
    }
}

context(SolutionApiFacade, TestDataStorage)
fun executeScript(file: String) {
    val script = testCaseSourceDirectory.combine(file)
    script.copyTo(project.solutionDirectory.combine("Assets", file))

    frameworkLogger.info("Executing script '$file'")
    project.solution.frontendBackendModel.refreshUnityModel()
}

fun FrontendBackendModel.refreshUnityModel() {
    frameworkLogger.info("Refreshing unity model")
    refresh.fire(true)
}

fun SolutionApiFacade.refreshUnityModel() = frontendBackendModel.refreshUnityModel()

private fun SolutionApiFacade.executeMethod(runMethodData: RunMethodData): RunMethodResult {
    frameworkLogger.info(
        "Executing method ${runMethodData.methodName} from ${runMethodData.typeName} (assembly: ${runMethodData.assemblyName})")
    val runMethodResult = frontendBackendModel.runMethodInUnity.callSynchronously(runMethodData, frontendBackendModel.protocolOrThrow)!!
    assertTrue(runMethodResult.success, "runMethodResult.success is false \n${runMethodResult.message} \n${runMethodResult.stackTrace}")
    frameworkLogger.info("Method was executed")
    return runMethodResult
}

fun waitConnectionToUnityEditor(project: Project) {
    frameworkLogger.info("Waiting for connection between Unity editor and Rider")
    waitAndPump(project.lifetime,
                {
                    project.isConnectedToEditor()
                    && project.solution.frontendBackendModel.unityEditorState.valueOrDefault(
                        UnityEditorState.Disconnected) != UnityEditorState.Disconnected
                },
                unityDefaultTimeout) { "unityHost is not initialized." }
    frameworkLogger.info("unityHost is initialized.")
}

fun checkSweaInSolution(project: Project) {
    changeFileSystem2(project) { arrayOf(project.solutionDirectory.resolve("Assembly-CSharp.csproj")) }
    checkSwea(project, 0)
}

fun SolutionApiFacade.checkSweaInSolution() = checkSweaInSolution(project)

fun SolutionApiFacade.executeIntegrationTestMethod(methodName: String) =
    executeMethod(RunMethodData("Assembly-CSharp-Editor", "Editor.IntegrationTestHelper", methodName))

fun printEditorLogEntry(stream: PrintStream, logEvent: LogEvent) {
    if (logEvent.type == LogEventType.Message) {
        stream.println("${logEvent.type}, ${logEvent.mode}, ${logEvent.message}\n " +
                       logEvent.stackTrace.replace(Regex(" \\(at .+\\)"), ""))
    }
}

//endregion

//region Playing

fun SolutionApiFacade.play(waitForPlay: Boolean = true) {
    frameworkLogger.info("Start playing in unity editor")
    frontendBackendModel.playControls.play.set(true)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun SolutionApiFacade.pause(waitForPause: Boolean = true) {
    frameworkLogger.info("Pause unity editor")
    frontendBackendModel.playControls.pause.set(true)
    if (waitForPause) waitForUnityEditorPauseMode()
}

// "2000000" is default log message in NewBehaviourScript.Update() in test solutions
fun SolutionApiFacade.step(waitForStep: Boolean = true, logMessageAfterStep: String = "2000000") {
    frameworkLogger.info("Make step in unity editor")
    if (waitForStep) {
        waitForEditorLogsAfterAction(logMessageAfterStep) { frontendBackendModel.playControls.step.fire(Unit) }
    }
    else {
        frontendBackendModel.playControls.step.fire(Unit)
    }
}

fun SolutionApiFacade.stopPlaying(waitForIdle: Boolean = true) {
    frameworkLogger.info("Stop playing in unity editor")
    frontendBackendModel.playControls.play.set(false)
    if (waitForIdle) waitForUnityEditorIdleMode()
}

fun SolutionApiFacade.unpause(waitForPlay: Boolean = true) {
    frameworkLogger.info("Unpause unity editor")
    frontendBackendModel.playControls.pause.set(false)
    if (waitForPlay) waitForUnityEditorPlayMode()
}

fun SolutionApiFacade.waitForUnityEditorPlayMode() = waitForUnityEditorState(UnityEditorState.Play)

fun SolutionApiFacade.waitForUnityEditorPauseMode() = waitForUnityEditorState(UnityEditorState.Pause)

fun SolutionApiFacade.waitForUnityEditorIdleMode() = waitForUnityEditorState(UnityEditorState.Idle)

fun SolutionApiFacade.waitForEditorLogsAfterAction(vararg expectedMessages: String,
                                                                         action: () -> Unit): List<LogEvent> {
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

private fun SolutionApiFacade.waitForUnityEditorState(editorState: UnityEditorState) {
    frameworkLogger.info("Waiting for unity editor in state '$editorState'")
    waitAndPump(unityActionsTimeout, { frontendBackendModel.unityEditorState.valueOrNull == editorState })
    { "Unity editor isn't in state '$editorState', actual state '${frontendBackendModel.unityEditorState.valueOrNull}'" }
}

fun SolutionApiFacade.restart() {
    stopPlaying()
    play()
}

//endregion

//region RunDebug

fun UnityPlayerDebuggerTestBase.runUnityPlayerAndAttachDebugger(
    playerFile: File,
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null) {

    assert(playerFile.exists())
    var startGameExecutable: Process? = null
    var session: XDebugSession? = null

    try {
        val pair = startUnityStandaloneProject(playerFile, testMethod.logDirectory.resolve("UnityPlayer.log"))
        val unityProcess: UnityProcess? = pair.first
        startGameExecutable = pair.second

        assertNotNull(unityProcess)
        attachToUnityProcess(project, unityProcess)

        session = waitForNotNull(UnityPlayerDebuggerTestBase.collectTimeout, "Debugger session wasn't started") {
            XDebuggerManager.getInstance(project).debugSessions.firstOrNull()
        }

        assertNotNull(session, "Debug session is null after waiting for its initialization")

        if (goldFile != null) {
            executeWithGold(goldFile) {
                val context = DebugTestExecutionContext(it, session)
                context.test()
                flushQueues()
            }
        }
        else {
            val context = DebugTestExecutionContext(NullPrintStream, session)
            context.test()
        }
    }
    catch (e: Throwable) {
        logger.error(e)
    }
    finally {
        assertNotNull(session)
        shutdownDebuggerSession(session, true)

        if (startGameExecutable != null && startGameExecutable.isAlive)
            startGameExecutable.destroyProcess(Duration.ofSeconds(3))
    }
}

private fun UnityPlayerDebuggerTestBase.startUnityStandaloneProject(playerFile: File, logPath: File)
    : Pair<UnityProcess?, Process?> {

    val startGameExecutable = startGameExecutable(playerFile, logPath)
    assertNotNull(startGameExecutable,"Failed to start game executable")

    var unityProcess: UnityProcess? = null
    val job = (project as ComponentManagerEx).getCoroutineScope().launch {
        unityProcess = discoverDebuggableUnityProcess(project.lifetime) {
            it.projectName == project.solutionName
        }
    }

    pumpMessages(DebugTestExecutionContext.waitForStopTimeout) {
        job.isCompleted
    }

    return Pair(unityProcess, startGameExecutable)
}

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
    goldFile: File? = null,
    customSuffixes: List<String> = emptyList()
) = attachDebuggerToUnityEditor(project, true, beforeRun, test, goldFile, customSuffixes)

context(SolutionApiFacade, TestDataStorage)
fun attachDebuggerToUnityEditorAndPlay(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null) = attachDebuggerToUnityEditorAndPlay(project, beforeRun, test, goldFile, customGoldSuffixes)

fun attachDebuggerToUnityEditor(
    project: Project,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null
) = attachDebuggerToUnityEditor(project, false, beforeRun, test, goldFile)

fun SolutionApiFacade.attachDebuggerToUnityEditor(
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null) = attachDebuggerToUnityEditor(project, beforeRun, test, goldFile)

private fun attachDebuggerToUnityEditor(
    project: Project,
    andPlay: Boolean,
    beforeRun: ExecutionEnvironment.() -> Unit = {},
    test: DebugTestExecutionContext.() -> Unit,
    goldFile: File? = null,
    customSuffixes: List<String> = emptyList()
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
        executeWithGold(goldFile, customSuffixes) {
            debugProgram(project, it, beforeRun, test, {}, true)
        }
    }
    else {
        debugProgram(project, NullPrintStream, beforeRun, test, {}, true)
    }
}

fun toggleUnityPausepoint(project: Project,
                                                   projectFile: String,
                                                   lineNumber: Int,
                                                   condition: String = ""): XLineBreakpoint<DotNetLineBreakpointProperties> {
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
    pausepoint.setCondition(condition)
    frameworkLogger.info("Set pausepoint condition: '$condition'")

    return pausepoint
}

//endregion

//region UnitTesting

fun SolutionApiFacade.preferStandaloneNUnitLauncherInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.NUnit)

fun SolutionApiFacade.preferEditModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.EditMode)

fun SolutionApiFacade.preferPlayModeInTests() =
    selectUnitTestLaunchPreference(UnitTestLaunchPreference.PlayMode)

private fun SolutionApiFacade.selectUnitTestLaunchPreference(preference: UnitTestLaunchPreference) {
    frameworkLogger.info("Selecting unit test launch preference '$preference'")
    frontendBackendModel.unitTestPreference.set(preference)
}

//endregion

//region for Unity versions gold file

fun getGoldFileUnityDependentSuffix(engineVersion: EngineVersion): String {
    return "_${engineVersion.version.lowercase()}"
}

fun getUnityDependentGoldFile(engineVersion: EngineVersion, testFile: File): File {
    val suffix = getGoldFileUnityDependentSuffix(engineVersion)
    val fileWithNameSuffix = testFile.getFileWithNameSuffix(suffix)
    val goldFileWithSuffix = fileWithNameSuffix.getGoldFile()

    if (goldFileWithSuffix.exists()) {
        return goldFileWithSuffix
    }
    return testFile
}

//endregion

//region Interface

val SolutionApiFacade.frontendBackendModel: FrontendBackendModel
    get() = project.solution.frontendBackendModel
//endregion

fun SolutionApiFacade.waitForUnityPackagesCache() {
    waitAndPump(project.lifetime,
                { project.solution.frontendBackendModel.isUnityPackageManagerInitiallyIndexFinished.valueOrDefault(false) },
                Duration.ofSeconds(10), { "Deferred caches are not completed" })
}