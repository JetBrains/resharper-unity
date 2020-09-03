package base.integrationTests

import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.newvfs.impl.VfsRootAccess
import com.intellij.util.io.exists
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.RdUnityModel
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import org.testng.annotations.AfterClass
import org.testng.annotations.BeforeMethod
import java.io.File
import java.nio.file.Files
import java.nio.file.Paths
import java.time.Duration

abstract class IntegrationTestBase : BaseTestWithSolution() {

    companion object {
        val defaultTimeout: Duration = Duration.ofSeconds(120)
        val actionsTimeout: Duration = Duration.ofSeconds(20)
    }

    private val lifetimeDefinition = LifetimeDefinition()
    val lifetime = lifetimeDefinition.lifetime

    val rdUnityModel: RdUnityModel
        get() = project.solution.rdUnityModel

    private val unityPath = when {
        SystemInfo.isWindows -> "C:/Program Files/Unity"
        SystemInfo.isMac -> "/Applications/Unity"
        else -> throw Exception("Not implemented")
    }

    override val waitForCaches = true
    override fun preprocessTempDirectory(tempDir: File) {
        VfsRootAccess.allowRootAccess(lifetimeDefinition.createNestedDisposable(), unityPath)

        // Needed, because com.jetbrains.rider.plugins.unity.ProtocolInstanceWatcher
        //  isn't initialized without correct unity file structure
        val libraryFolder = Paths.get(tempDir.toString(), "Library")
        if (!libraryFolder.exists()) {
            Files.createDirectory(libraryFolder)
        }
    }

    @BeforeMethod
    fun setUpRdUnityModelSettings() {
        if (!rdUnityModel.riderFrontendTests.valueOrDefault(false)) {
            rdUnityModel.riderFrontendTests.set(true)
        }
    }

    @AfterClass
    fun terminateLifetime() {
        lifetimeDefinition.terminate()
    }
}