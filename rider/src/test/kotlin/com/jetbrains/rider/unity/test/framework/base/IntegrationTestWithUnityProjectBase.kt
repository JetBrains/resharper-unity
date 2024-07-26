package com.jetbrains.rider.unity.test.framework.base

import com.intellij.openapi.util.io.FileUtil
import com.intellij.util.WaitFor
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.unity.test.framework.api.getEngineExecutableInstallationPath
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.api.startUnity
import com.jetbrains.rider.unity.test.framework.riderPackageVersion
import org.testng.annotations.BeforeMethod
import java.io.File
import java.io.FileNotFoundException
import java.time.Duration

/**
 * Class should be used when we want to start Unity Editor before Rider to get csproj/sln generated
 * IntegrationTestWithGeneratedSolutionBase always opens Rider first and expect sln files to exist in the test-data
 */
abstract class IntegrationTestWithUnityProjectBase : IntegrationTestWithGeneratedSolutionBase() {
    private lateinit var unityProjectPath: File
    protected abstract val majorVersion: EngineVersion
    private val unityExecutable: File by lazy { getEngineExecutableInstallationPath(majorVersion) }
    private val packageManifestPath = "/Packages/manifest.json"
    private val riderPackageTag = "{{VERSION}}"

    override val testGoldFile: File
        get() = getUnityDependentGoldFile(majorVersion, super.testGoldFile).takeIf { it.exists() }
                ?: getUnityDependentGoldFile(
                    majorVersion,
                    File(super.testGoldFile.path.replace(this::class.simpleName.toString(), ""))
                )

    private fun putUnityProjectToTempTestDir(
        solutionDirectoryName: String,
        filter: ((File) -> Boolean)? = null
    ): File {
        val solutionName: String = File(solutionDirectoryName).name
        val workDirectory = File(tempTestDirectory, solutionName)
        val sourceDirectory = File(solutionSourceRootDirectory, solutionDirectoryName)
        // Copy solution from sources
        FileUtil.copyDir(sourceDirectory, workDirectory, filter)
        // Copy additional files
        copyAdditionalFilesToProject(solutionName, workDirectory)

        workDirectory.isDirectory.shouldBeTrue("Expected '${workDirectory.absolutePath}' to be a directory")
        return workDirectory
    }

    private fun copyAdditionalFilesToProject (solutionName: String, workDirectory: File) {
        var helperFileDirectory = testDataDirectory.resolve("additionalFiles").resolve(solutionName)
        val destinationPath = workDirectory.resolve("Assets").resolve("Editor")
        if (!helperFileDirectory.exists()) {
            helperFileDirectory = testDataDirectory.resolve("additionalFiles").resolve("integrationTestHelper")
        }
        helperFileDirectory.listFiles()?.forEach { file ->
            file.copyTo(destinationPath.resolve(file.name))
        }
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

    //Set the latest version of rider package in tests projects via replacing {{VERSION}} marker in manifest.json
    //riderPackageVersion is stored in UnityTestEnvironment.kt
    private fun setRiderPackageVersion (sourceDirectory: File) {
        val file = File("${sourceDirectory.path}$packageManifestPath").takeIf { it.isFile } ?: throw FileNotFoundException("Cannot find $packageManifestPath")
        val content = file.readText()
        val updatedContent = content.replace(riderPackageTag, riderPackageVersion)
        file.writeText(updatedContent)
    }

    @BeforeMethod(alwaysRun = true)
    override fun setUpTestCaseSolution() {
        setRiderPackageVersion(File(solutionSourceRootDirectory, testSolution))
        unityProjectPath = putUnityProjectToTempTestDir(testSolution, null)
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