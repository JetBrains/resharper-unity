package base.integrationTests

import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import java.io.File

abstract class IntegrationTestWithSolutionBase : BaseTestWithSolution(), IntegrationTestWithRdUnityModel {
    override val waitForCaches = true

    override val rdUnityModel: RdUnityModel
        get() = project.solution.rdUnityModel

    private lateinit var lifetimeDefinition: LifetimeDefinition

    override fun preprocessTempDirectory(tempDir: File) {
        lifetimeDefinition = LifetimeDefinition()
        allowUnityPathVfsRootAccess(lifetimeDefinition)
        createLibraryFolderIfNotExist(tempDir)
    }

    @AfterMethod
    fun terminateLifetimeDefinition() {
        lifetimeDefinition.terminate()
    }

    @BeforeMethod
    fun setUpRdUnityModelSettings() {
        activateRiderFrontendTest()
    }
}