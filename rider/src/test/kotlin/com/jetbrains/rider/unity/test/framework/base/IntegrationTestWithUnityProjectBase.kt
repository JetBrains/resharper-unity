package com.jetbrains.rider.unity.test.framework.base

import com.intellij.openapi.util.io.FileUtil
import com.intellij.util.WaitFor
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.unity.test.framework.UnityVersion
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.api.getUnityExecutableInstallationPath
import com.jetbrains.rider.unity.test.framework.api.startUnity
import org.testng.annotations.BeforeMethod
import java.io.File
import java.time.Duration

/**
 * Class should be used when we want to start Unity Editor before Rider to get csproj/sln generated
 * IntegrationTestWithGeneratedSolutionBase always opens Rider first and expect sln files to exist in the test-data
 */
abstract class IntegrationTestWithUnityProjectBase : IntegrationTestWithGeneratedSolutionBase() {
    private lateinit var unityProjectPath: File
    protected abstract val unityMajorVersion: UnityVersion
    private val unityExecutable: File by lazy { getUnityExecutableInstallationPath(unityMajorVersion) }
    private val unityGoldFile : File by lazy {getUnityDependentGoldFile(unityMajorVersion, File(testClassDataDirectory.parent, testMethod.name))}
    override val testGoldFile: File
        get() = unityGoldFile.takeIf { it.exists() } ?: super.testGoldFile

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

    private fun waitForSlnGeneratedByUnity(
        unityProcessHandle: ProcessHandle,
        slnDirPath: String,
        timeoutMinutes: Duration = Duration.ofMinutes(5L)
    ) {
        try {
            object : WaitFor(timeoutMinutes.toMillis().toInt(), 10000) {
                override fun condition(): Boolean {
                    val slnFiles = File(slnDirPath).listFiles { _, name -> name.endsWith(".sln") }
                    return !unityProcessHandle.isAlive && slnFiles != null && slnFiles.isNotEmpty()
                }
            }.assertCompleted("Sln/csproj structure has not been created by Unity in the batch mode")
        }
        finally {
            if (unityProcessHandle.isAlive) {
                frameworkLogger.info(
                    "Killing Unity process which did not generate csproj structure in ${timeoutMinutes.toMinutes()} minutes")
                unityProcessHandle.destroyForcibly()
            }
        }
    }

    @BeforeMethod(alwaysRun = true)
    override fun setUpTestCaseSolution() {
        unityProjectPath = putUnityProjectToTempTestDir(getSolutionDirectoryName(), null)
        val unityProcessHandle = startUnity(
            executable = unityExecutable.canonicalPath,
            projectPath = unityProjectPath.canonicalPath,
            withCoverage = false,
            resetEditorPrefs = resetEditorPrefs,
            useRiderTestPath = useRiderTestPath,
            batchMode = batchMode,
            generateSolution = true
        )

        //Generate sln and csproj
        frameworkLogger.info("Unity Editor has been started, waiting for sln/csproj structure to be generated")
        waitForSlnGeneratedByUnity(unityProcessHandle, unityProjectPath.canonicalPath)
        frameworkLogger.info("Sln/csproj structure has been created, opening project in Rider")
        super.setUpTestCaseSolution()
    }

    @BeforeMethod(dependsOnMethods = ["setUpTestCaseSolution"])
    override fun startUnityProcessAndWait() {
        super.startUnityProcessAndWait()
    }

    @BeforeMethod(dependsOnMethods = ["setUpTestCaseSolution"])
    override fun setUpModelSettings() {
        super.setUpModelSettings()
    }

    @BeforeMethod(dependsOnMethods = ["startUnityProcessAndWait"])
    override fun waitForUnityRunConfigurations() {
        super.waitForUnityRunConfigurations()
    }

    @BeforeMethod(dependsOnMethods = ["waitForUnityRunConfigurations"])
    override fun buildSolutionAfterUnityStarts() {
        super.buildSolutionAfterUnityStarts()
    }
}