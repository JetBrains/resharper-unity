package base.integrationTests

import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolutionBase
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithConsoleBuild
import com.jetbrains.rider.test.scriptingApi.buildSolutionWithReSharperBuild
import org.testng.annotations.AfterClass
import org.testng.annotations.BeforeMethod
import java.time.Duration

abstract class IntegrationTestWithReuseSolutionBase : BaseTestWithSolutionBase(), IntegrationTestWithRdUnityModel {
    protected open val withCoverage: Boolean
        get() = false

    protected open val resetEditorPrefs: Boolean
        get() = false

    protected open val useRiderTestPath: Boolean
        get() = false

    protected open val batchMode: Boolean
        get() = true

    protected abstract val solutionName: String

    protected open val openSolutionParams: OpenSolutionParams
        get() = OpenSolutionParams().apply {
            waitForCaches = true
            preprocessTempDirectory = {
                allowUnityPathVfsRootAccess()
                createLibraryFolderIfNotExist(it)
            }
            backendLoadedTimeout = Duration.ofSeconds(60)
        }

    override val clearCaches: Boolean
        get() = false

    override val testCaseNameToTempDir: String
        get() = "tempTestDir"

    private var myUnityProcessHandle: ProcessHandle? = null
    val unityProcessHandle: ProcessHandle
        get() = myUnityProcessHandle!!

    private var myProject: Project? = null
    val project: Project
        get() = myProject!!

    override val rdUnityModel: RdUnityModel
        get() = project.solution.rdUnityModel

    @BeforeMethod(alwaysRun = true)
    fun openSolutionIfNeeded() {
        val solution = testMethod.environment.solution ?: solutionName
        if (solution != activeSolution) {
            if (myProject != null) {
                val oldSolutionDirectory = activeSolutionDirectory
                closeSolution(project)
                myProject = null
                oldSolutionDirectory.deleteRecursively()
            }
            myProject = openSolution(solution, openSolutionParams) {
                notificationList.add(it)
            }
            installPlugin(project)
            activateRiderFrontendTest()
        }

        if (myUnityProcessHandle != null) {
            killUnity(project, unityProcessHandle)
            checkSweaInSolution(project)
        }

        if (myUnityProcessHandle == null) {
            myUnityProcessHandle = startUnity(project, withCoverage, resetEditorPrefs, useRiderTestPath, batchMode)
            waitFirstScriptCompilation(project)
            waitConnectionToUnityEditor(project)

            buildSolutionWithConsoleBuild(project)
            buildSolutionWithReSharperBuild(project)
        }
    }

    @AfterClass(alwaysRun = true)
    fun killUnity() {
        try {
            stopPlaying()
            killUnity(project, unityProcessHandle)
            checkSweaInSolution(project)
        } finally {
            myUnityProcessHandle = null
        }
    }

    @AfterClass(alwaysRun = true, dependsOnMethods = ["killUnity"])
    fun closeSolution() {
        try {
            closeSolution(project)
        } finally {
            myProject = null
        }
    }
}