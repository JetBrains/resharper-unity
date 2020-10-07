package base.integrationTests

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.model.FrontendBackendModel
import com.jetbrains.rider.model.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File

abstract class IntegrationTestWithSolutionBase : BaseTestWithSolution(), IntegrationTestWithFrontendBackendModel {
    override val waitForCaches = true

    override val frontendBackendModel: FrontendBackendModel
        get() = project.solution.frontendBackendModel

    private lateinit var lifetimeDefinition: LifetimeDefinition

    override fun preprocessTempDirectory(tempDir: File) {
        lifetimeDefinition = LifetimeDefinition()
        allowUnityPathVfsRootAccess(lifetimeDefinition)
        createLibraryFolderIfNotExist(tempDir)
    }

    @AfterMethod(alwaysRun = true)
    fun terminateLifetimeDefinition() {
        lifetimeDefinition.terminate()
    }

    @BeforeMethod
    fun setUpModelSettings() {
        activateRiderFrontendTest()
    }
}