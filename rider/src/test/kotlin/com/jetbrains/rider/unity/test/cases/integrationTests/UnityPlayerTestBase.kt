package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.io.FileUtil
import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.plugins.unity.run.UnityPlayerListener
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.facades.solution.RiderExistingSolutionApiFacade
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.allowUnityPathVfsRootAccess
import com.jetbrains.rider.test.scriptingApi.createLibraryFolderIfNotExist
import com.jetbrains.rider.test.scriptingApi.getEngineExecutableInstallationPath
import com.jetbrains.rider.test.scriptingApi.setRiderPackageVersion
import com.jetbrains.rider.test.unity.EngineVersion
import com.jetbrains.rider.test.unity.riderPackageVersion
import com.jetbrains.rider.unity.test.framework.api.activateRiderFrontendTest
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import kotlinx.coroutines.CompletableDeferred
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File
import kotlin.test.assertNotNull

abstract class UnityPlayerTestBase(private val engineVersion: EngineVersion) : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        super.modifyOpenSolutionParams(params)
        params.waitForCaches = true
        params.preprocessTempDirectory = {
            lifetimeDefinition = LifetimeDefinition()
            allowUnityPathVfsRootAccess(lifetimeDefinition)
            createLibraryFolderIfNotExist(it)
        }
    }

    private val unityExecutable: File by lazy { getEngineExecutableInstallationPath(engineVersion) }
    private val unityMajorVersion = this.engineVersion
    private lateinit var unityProjectPath: File
    private lateinit var lifetimeDefinition: LifetimeDefinition

    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger
    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DotsDebuggerTest::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testProcessor.testMethod.name).combine("source")

    private fun buildUnityPlayer() {
        val buildDir = File(unityProjectPath, "Builds")
        val buildTarget = when {
            SystemInfo.isMac -> "OSXUniversal"
            SystemInfo.isWindows -> "Win64"
            SystemInfo.isLinux -> "Linux64"
            else -> error("Unsupported OS for Unity player build: ${SystemInfo.getOsName()}")
        }

        val process = ProcessBuilder(
            unityExecutable.absolutePath,
            "-batchmode", "-quit",
            "-projectPath", unityProjectPath.absolutePath,
            "-executeMethod", "BuildScript.Build",
            "-logFile", File(testMethod.logDirectory, "PlayerBuild.log").absolutePath,
            "-buildTarget", buildTarget
        ).start()

        process.waitFor()
        assert(process.exitValue() == 0) { "Unity Build failed! Check logs at: ${buildDir.absolutePath}/build.log" }
    }

    protected fun getPlayerFile(): File {
        val buildDir = File(unityProjectPath, "Builds")
        val gameFilePath = if (SystemInfo.isMac) "SimpleUnityGame.app" else "SimpleUnityGame.exe"
        val gameFullPath = if (SystemInfo.isMac) {
            buildDir
                .resolve(gameFilePath)
                .resolve("Contents/MacOS")
                .resolve(gameFilePath.removeSuffix(".app"))
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
            return getUnityDependentGoldFile(unityMajorVersion, super.testGoldFile).takeIf { it.exists() }
                   ?: getUnityDependentGoldFile(
                       unityMajorVersion,
                       File(super.testGoldFile.path.replace(this::class.simpleName.toString(), ""))
                   )
        }

    override val solutionApiFacade: SolutionApiFacade by lazy { RiderExistingSolutionApiFacade() }

    private fun putUnityProjectToTempTestDir(
        solutionDirectoryName: String,
        filter: ((File) -> Boolean)? = null
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
        buildUnityPlayer()
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

    suspend fun discoverDebuggableUnityProcess(lifetime: Lifetime, filter: (UnityProcess) -> Boolean): UnityProcess {
        val result = CompletableDeferred<UnityProcess>()
        lifetime.onTermination { result.cancel() }

        logger.info("Starting UnityPlayerListener")
        try {

            UnityPlayerListener()
                .startListening(lifetime,
                                {
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