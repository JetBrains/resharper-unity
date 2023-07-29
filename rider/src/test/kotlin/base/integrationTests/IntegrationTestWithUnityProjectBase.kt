package base.integrationTests

import com.intellij.openapi.util.io.FileUtil
import com.intellij.util.WaitFor
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.test.asserts.shouldBeTrue
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File
import java.time.Duration

/**
 * Class should be used when we want to start Unity Editor before Rider to get csproj/sln generated
 * IntegrationTestWithGeneratedSolutionBase always opens Rider first and expect sln files to exist in the test-data
 */
abstract class IntegrationTestWithUnityProjectBase : IntegrationTestWithSolutionBase() {
    private lateinit var unityProjectPath: File
    protected abstract val unityMajorVersion: UnityVersion
    private val unityExecutable: File by lazy { getUnityExecutableInstallationPath(unityMajorVersion) }

    protected open val withCoverage: Boolean
        get() = false
    protected open val resetEditorPrefs: Boolean
        get() = false
    protected open val useRiderTestPath: Boolean
        get() = false
    protected open val batchMode: Boolean
        get() = true

    private lateinit var unityProcessHandle: ProcessHandle

    private fun putUnityProjectToTempTestDir(
        solutionDirectoryName: String,
        filter: ((File) -> Boolean)? = null
    ): File {

        val workDirectory = File(tempTestDirectory, solutionDirectoryName)
        val sourceDirectory = File(solutionSourceRootDirectory, solutionDirectoryName)
        // Copy solution from sources
        FileUtil.copyDir(sourceDirectory, workDirectory, filter)
        workDirectory.isDirectory.shouldBeTrue("Expected '${workDirectory.absolutePath}' to be a directory")

        return workDirectory
    }

    private fun waitForSlnGeneratedByUnityFile(slnDirPath: String, timeoutMinutes: Duration = Duration.ofMinutes(5L)) {
        object : WaitFor(timeoutMinutes.toMillis().toInt(), 10000) {
            override fun condition(): Boolean {
                val slnFiles = File(slnDirPath).listFiles { _, name -> name.endsWith(".sln") }
                val startFile = File(slnDirPath, ".start")
                return slnFiles != null && slnFiles.isNotEmpty() && startFile.exists()
            }
        }.assertCompleted("Sln/csproj structure has been created, opening project in Rider")
    }

    @BeforeMethod(alwaysRun = true)
    override fun setUpTestCaseSolution() {
        unityProjectPath = putUnityProjectToTempTestDir(getSolutionDirectoryName(), null)
        val unityTestEnvironment = testMethod.unityEnvironment
        unityProcessHandle = when {
            unityTestEnvironment != null ->
                startUnity(
                    executable = unityExecutable.canonicalPath,
                    projectPath = unityProjectPath.canonicalPath,
                    withCoverage = unityTestEnvironment.withCoverage,
                    resetEditorPrefs = unityTestEnvironment.resetEditorPrefs,
                    useRiderTestPath = unityTestEnvironment.useRiderTestPath,
                    batchMode = unityTestEnvironment.batchMode
                )

            else ->
                startUnity(
                    executable = unityExecutable.canonicalPath,
                    projectPath = unityProjectPath.canonicalPath,
                    withCoverage = withCoverage,
                    resetEditorPrefs = resetEditorPrefs,
                    useRiderTestPath = useRiderTestPath,
                    batchMode = batchMode
                )
        }

        //Generate sln and csproj
        frameworkLogger.info("Unity Editor has been started, waiting for sln/csproj structure to be generated")
        waitForSlnGeneratedByUnityFile(unityProjectPath.canonicalPath)
        super.setUpTestCaseSolution()
    }

    @BeforeMethod(dependsOnMethods = ["setUpTestCaseSolution"])
    override fun setUpModelSettings() {
        activateRiderFrontendTest()
    }

    @BeforeMethod(dependsOnMethods = ["setUpModelSettings"])
    fun waitForUnityConnection() {
        waitFirstScriptCompilation(project)
        waitConnectionToUnityEditor(project)
    }

    @BeforeMethod(dependsOnMethods = ["waitForUnityConnection"])
    fun waitForUnityRunConfigurations() {
        refreshUnityModel()
        waitForUnityRunConfigurations(project)
    }

    @BeforeMethod(dependsOnMethods = ["waitForUnityConnection"])
    fun buildSolutionAfterUnityStarts() {
        buildSolutionWithReSharperBuild(project, ignoreReferencesResolve = true)
    }

    @AfterMethod(alwaysRun = true)
    fun killUnityAndCheckSwea() {
        if (::unityProcessHandle.isInitialized) {
            killUnity(project, unityProcessHandle)
        }
        checkSweaInSolution()
    }
}