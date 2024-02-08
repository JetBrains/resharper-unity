package com.jetbrains.rider.unity.test.cases.integrationTests

import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.io.FileUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.UnityPlayerListener
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.packages.ZipFilePackagePreparer
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.refreshFileSystem
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.*
import kotlinx.coroutines.CompletableDeferred
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File
import kotlin.test.assertNotNull

abstract class UnityPlayerTestBase(private val unityVersion: UnityVersion,
                                   private val buildNames: Map<String, String>) : BaseTestWithSolution(), IntegrationTestWithFrontendBackendModel {
    override val waitForCaches = true
    private val unityMajorVersion = this.unityVersion
    private lateinit var unityProjectPath: File
    private lateinit var lifetimeDefinition: LifetimeDefinition

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(DotsDebuggerTestBase::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testStorage.testMethod.name).combine("source")
    override val frontendBackendModel: FrontendBackendModel
        get() = project.solution.frontendBackendModel

    protected fun getGameFileName(): String? {
        if(SystemInfo.isMac)
            return buildNames[UnityPlayerDebuggerTestBase.macOS]
        if(SystemInfo.isWindows)
            return buildNames[UnityPlayerDebuggerTestBase.winOS]
        return null
    }


    override val testGoldFile: File
        get() {
            return getUnityDependentGoldFile(unityMajorVersion, super.testGoldFile).takeIf { it.exists() }
                   ?: getUnityDependentGoldFile(
                     unityMajorVersion,
                     File(super.testGoldFile.path.replace(this::class.simpleName.toString(), ""))
                   )
        }

    private fun putUnityProjectToTempTestDir(
        solutionDirectoryName: String,
        filter: ((File) -> Boolean)? = null
    ): File {
        val solutionName: String = File(solutionDirectoryName).name
        val workDirectory = File(tempTestDirectory, solutionName)
        val sourceDirectory = File(solutionSourceRootDirectory, solutionDirectoryName)
        // Copy solution from sources
      FileUtil.copyDir(sourceDirectory, workDirectory, filter)
        workDirectory.isDirectory.shouldBeTrue("Expected '${workDirectory.absolutePath}' to be a directory")

        return workDirectory
    }

    override fun preprocessTempDirectory(tempDir: File) {
        lifetimeDefinition = LifetimeDefinition()
      allowUnityPathVfsRootAccess(lifetimeDefinition)
      createLibraryFolderIfNotExist(tempDir)
    }

    @BeforeMethod(alwaysRun = true)
    override fun setUpTestCaseSolution() {
        unityProjectPath = putUnityProjectToTempTestDir(getSolutionDirectoryName(), null)
        super.setUpTestCaseSolution()
      prepareAssemblies(project, activeSolutionDirectory)
        downloadGameFiles(project, activeSolutionDirectory)
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

    private fun downloadGameFiles(project: Project, activeSolutionDirectory: File) {
        val zipFileName = getGameFileName()
        assertNotNull(zipFileName)
        val zipFilePackage by ZipFilePackagePreparer(zipFileName)
        //moving all UnityEngine* and UnityEditor*, netstandard and mscorlib ref-asm dlls to test solution folder
        for (file in zipFilePackage.root.listFiles()!!) {
            val target = activeSolutionDirectory.combine(file.name)
            file.copyRecursively(target, true)
        }
      refreshFileSystem(project)
    }

    fun waitGameProcess(lifetime: Lifetime, filter: (UnityProcess) -> Boolean): CompletableDeferred<UnityProcess> {
        val result = CompletableDeferred<UnityProcess>()
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

        return result
    }

    fun startGameExecutable(file: File): Process? {
        assert(file.exists())
        try {
            logger.info("Starting game process:$file")
            val process: Process? = ProcessBuilder(mutableListOf(file.path, "-batchmode")).start()
            if (process != null) {
                logger.info("Game process started:${process.info()}")
                return process
            }

            logger.error("Failed to start game process$file")
            return null
        }
        catch (exception: Throwable) {
            logger.error("Failed to start game process$file", exception)
            return null
        }
    }
}