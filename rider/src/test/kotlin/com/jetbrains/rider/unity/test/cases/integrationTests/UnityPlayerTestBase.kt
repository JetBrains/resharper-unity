package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.util.io.FileUtil
import com.intellij.openapi.util.io.toCanonicalPath
import com.intellij.util.system.OS
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.plugins.unity.run.UnityPlayerListener
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.enums.EngineVersion
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.enums.UnityBackend
import com.jetbrains.rider.test.facades.solution.RiderExistingSolutionApiFacade
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.FirewallHelper.addAllowRuleToFirewall
import com.jetbrains.rider.unity.test.framework.FirewallHelper.removeRuleFromFirewall
import com.jetbrains.rider.unity.test.framework.api.activateRiderFrontendTest
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rider.unity.test.framework.base.BaseTestWithUnitySetup
import kotlinx.coroutines.CompletableDeferred
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File
import java.util.concurrent.TimeUnit
import kotlin.io.path.pathString
import kotlin.test.assertNotNull

abstract class UnityPlayerTestBase() : BaseTestWithUnitySetup() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.waitForCaches = true
        params.preprocessTempDirectory = {
            lifetimeDefinition = LifetimeDefinition()
            allowUnityPathVfsRootAccess(lifetimeDefinition)
            createLibraryFolderIfNotExist(it)
        }
    }

    override val solutionApiFacade: SolutionApiFacade by lazy { RiderExistingSolutionApiFacade() }

    protected val engineVersion: EngineVersion
        get() {
            val settings = getUnityTestSettingsAnnotation()
            return settings.tuanjieVersion.takeIf { it != TuanjieVersion.NONE } ?: settings.unityVersion
        }

    protected val unityBackend: UnityBackend
        get() = getUnityTestSettingsAnnotation().unityBackend


    private val unityExecutable: File by lazy { getEngineExecutableInstallationPath(engineVersion) }
    private lateinit var unityProjectPath: File
    private lateinit var lifetimeDefinition: LifetimeDefinition
    private lateinit var unityPlayerFile: File

    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger
    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DotsDebuggerTest::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testProcessor.testMethod.name).combine("source")

    private fun buildUnityPlayer(unityBackend: UnityBackend) {
        val buildLogFile = testMethod.logDirectory.combine("PlayerBuild.log")
        val buildTarget = when (OS.CURRENT) {
            OS.macOS -> "OSXUniversal"
            OS.Windows -> "Win64"
            OS.Linux -> "Linux64"
            else -> error("Unsupported OS for Unity player build: ${OS.CURRENT}")
        }

        val process = ProcessBuilder(
            unityExecutable.absolutePath,
            "-batchmode", "-quit",
            "-projectPath", unityProjectPath.absolutePath,
            "-executeMethod", "BuildScript.Build",
            "-logFile", buildLogFile.toCanonicalPath(),
            "-buildTarget", buildTarget,
            "-backend", unityBackend.toString()
        ).start()
        try {
            process.waitFor(5, TimeUnit.MINUTES)
            assert(process.exitValue() == 0) { "Unity Build failed! Check logs at: ${buildLogFile.toRealPath().pathString}" }
            // On macOS need to explicitly allow listening to the incoming connections
            if (OS.CURRENT == OS.macOS) {
                unityPlayerFile = getPlayerFile()
                addAllowRuleToFirewall(unityPlayerFile.canonicalPath)
            }
        } finally {
          process.destroyForcibly()
        }
    }

    protected fun getPlayerFile(): File {
        val buildDir = File(unityProjectPath, "Builds")
        val executableName =  unityProjectPath.name
        val gameFilePath = when (OS.CURRENT) {
            OS.macOS -> "$executableName.app"
            OS.Windows -> "$executableName.exe"
            OS.Linux -> executableName
            else -> error("Unsupported OS for Unity player build: ${OS.CURRENT}")
        }
        val gameFullPath = if (OS.CURRENT == OS.macOS) {
            buildDir
                .resolve(gameFilePath)
                .resolve("Contents/MacOS")
                .resolve(executableName)
        } else {
            buildDir.resolve(gameFilePath)
        }
        require(gameFullPath.exists()) {
            "Built Unity player not found at expected path: ${gameFullPath.absolutePath}"
        }
        return gameFullPath
    }

    override val testGoldFile: File
        get() {
            return getUnityDependentGoldFile(engineVersion, super.testGoldFile, unityBackend.toString()).takeIf { it.exists() }
                   ?: getUnityDependentGoldFile(
                       engineVersion,
                       File(super.testGoldFile.path.replace(this::class.simpleName.toString(), "")),
                       unityBackend.toString().lowercase()
                   )
        }

    private fun putUnityProjectToTempTestDir(
        solutionDirectoryName: String,
        filter: ((File) -> Boolean)? = null,
    ): File {
        val solutionName: String = File(solutionDirectoryName).name
        val workDirectory = File(testWorkDirectory, solutionName)
        val sourceDirectory = File(solutionSourceRootDirectory, solutionDirectoryName)
        // Copy solution from sources
        FileUtil.copyDir(sourceDirectory, workDirectory, filter)
        workDirectory.isDirectory.shouldBeTrue("Expected '${workDirectory.absolutePath}' to be a directory")

        return workDirectory
    }

    @BeforeMethod(alwaysRun = true)
    override fun setUpTestCaseSolution() {
        unityProjectPath = putUnityProjectToTempTestDir(testMethod.solution!!.name, null)
        setRiderPackageVersion(unityProjectPath, riderPackageVersion)
        super.setUpTestCaseSolution()
        prepareAssemblies(project, activeSolutionDirectory)
        buildUnityPlayer(unityBackend)
    }

    @BeforeMethod(dependsOnMethods = ["setUpTestCaseSolution"])
    open fun setUpModelSettings() {
        activateRiderFrontendTest()
    }

    @AfterMethod(alwaysRun = true)
    fun terminateLifetimeDefinition() {
        if (::lifetimeDefinition.isInitialized && lifetimeDefinition.isAlive) {
            lifetimeDefinition.terminate()
        }
    }

    @AfterMethod
    fun cleanupFirewallRules() {
        if (OS.CURRENT == OS.macOS && ::unityPlayerFile.isInitialized) {
            removeRuleFromFirewall(unityPlayerFile.canonicalPath)
        }
    }

    suspend fun discoverDebuggableUnityProcess(lifetime: Lifetime, filter: (UnityProcess) -> Boolean): UnityProcess {
        val result = CompletableDeferred<UnityProcess>()
        lifetime.onTermination { result.cancel() }

        logger.info("Starting UnityPlayerListener")
        try {

            UnityPlayerListener()
                .startListening(lifetime,
                                {
                                    logger.info("#### Found non-matching Unity Player process:$it")
                                    if (filter(it) && !result.isCompleted) {
                                        logger.info("Found Unity Player process:$it")
                                        result.complete(it)
                                    }
                                },
                                {})
        }
        catch (exception: Throwable) {
            logger.error("Failed to find Unity Player process", exception)
            result.completeExceptionally(exception)
        }

        return result.await()
    }

    fun startGameExecutable(playerFile: File, logPath: File): Process? {
        assertNotNull(playerFile, "Game executable not found after build!")

        return try {
            logger.info("Starting game process: $playerFile")
            playerFile.setExecutable(true)
            val process = ProcessBuilder(mutableListOf(playerFile.path, "-logfile", logPath.toString(), "-batchMode")).start()
            logger.info("Game process started: ${process.info()}")
            process
        } catch (exception: Throwable) {
            logger.error("Failed to start game process $playerFile", exception)
            null
        }
    }
}
