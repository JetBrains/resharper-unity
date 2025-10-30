package com.jetbrains.rider.unity.test.framework.base

import com.jetbrains.rd.platform.diagnostics.LogTraceScenario
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.test.enums.EngineVersion
import com.jetbrains.rider.test.enums.TuanjieVersion
import com.jetbrains.rider.test.facades.solution.RiderExistingSolutionApiFacade
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.testData.InheritanceBasedTestDataStorage
import com.jetbrains.rider.test.framework.testData.TestDataStorage
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.getUnityDependentGoldFile
import com.jetbrains.rider.unity.test.framework.api.startUnity
import org.testng.annotations.BeforeMethod
import java.io.File
import java.time.Duration

abstract class IntegrationTestWithUnityProjectBase() : IntegrationTestWithGeneratedSolutionBase() {
    private lateinit var unityProjectPath: File

  protected val engineVersion: EngineVersion
    get() {
      val settings = getUnityTestSettingsAnnotation()
      return settings.tuanjieVersion.takeIf { it != TuanjieVersion.NONE } ?: settings.unityVersion
    }

    override val traceScenarios: Set<LogTraceScenario>
        get() = super.traceScenarios + LogTraceScenarios.Debugger
    
    private val unityExecutable: File by lazy { getEngineExecutableInstallationPath(engineVersion) }

    override val customGoldSuffixes: List<String>
        get() = listOf("_${engineVersion.version.lowercase()}")

    override val testGoldFile: File
        get() = getUnityDependentGoldFile(engineVersion, super.testGoldFile).takeIf { it.exists() }
                ?: getUnityDependentGoldFile(
                    engineVersion,
                    File(super.testGoldFile.path.replace(this::class.simpleName.toString(), ""))
                )
    override val testDataStorage: TestDataStorage by lazy { InheritanceBasedTestDataStorage(testProcessor) }


    override val solutionApiFacade: SolutionApiFacade by lazy { RiderExistingSolutionApiFacade() }

    @BeforeMethod
    override fun setUpTestCaseSolution() {
        unityProjectPath = putUnityProjectToTempTestDir(testMethod.solution!!.name, null, testWorkDirectory, solutionSourceRootDirectory,
                                                        testDataDirectory)
        setRiderPackageVersion(unityProjectPath, riderPackageVersion)

        val attemptsCount = 3
        for (i in 1..attemptsCount) {
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
            frameworkLogger.info(
                "Unity Editor has been started, waiting for sln/csproj structure to be generated, attempt:$i/$attemptsCount")
            val isSolutionGenerated = waitForSlnGeneratedByUnity(unityProcessHandle, unityProjectPath.canonicalPath,
                                                                 Duration.ofMinutes(2L * i))
            if (isSolutionGenerated)
                frameworkLogger.info("Sln/csproj structure has been created, opening project in Rider")
            else
                frameworkLogger.info("Sln/csproj structure hasn't been created")
        }
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