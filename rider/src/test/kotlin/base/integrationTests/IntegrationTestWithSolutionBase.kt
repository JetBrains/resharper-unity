package base.integrationTests

import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import org.testng.annotations.BeforeMethod
import java.io.File

abstract class IntegrationTestWithSolutionBase : BaseTestWithSolution(), IntegrationTestWithRdUnityModel {
    override val waitForCaches = true

    override val rdUnityModel: RdUnityModel
        get() = project.solution.rdUnityModel

    override fun preprocessTempDirectory(tempDir: File) {
        allowUnityPathVfsRootAccess()
        createLibraryFolderIfNotExist(tempDir)
    }

    @BeforeMethod
    fun setUpRdUnityModelSettings() {
        activateRiderFrontendTest()
    }
}