import base.integrationTests.prepareAssemblies
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.daemon.util.annotateDocumentWithHighlighterTags
import com.jetbrains.rdclient.daemon.util.backendAttributeIdOrThrow
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.markupContributor
import com.jetbrains.rider.test.scriptingApi.runSwea
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class InputSystemUnityEventModeTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "MarkupTestData"
    }

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
    }
    @Test
    fun usedCodeTest() {
        val projectLifetime = project.lifetime
        val model = project.solution.frontendBackendModel
        runSwea(project) // otherwise public methods are never marked unused
        waitAndPump(projectLifetime, { model.isDeferredCachesCompletedOnce.valueOrDefault(false) }, Duration.ofSeconds(10),
                    { "Deferred caches are not completed" })
        executeWithGold(testGoldFile) { ps ->
            withOpenedEditor("Assets/Scripts/Mechanics/PlayerController.cs") {
                ps.println(annotateDocumentWithHighlighterTags(markupContributor.markupAdapter,
                                                               valueFilter = { it.backendAttributeIdOrThrow == "ReSharper Dead Code" }))
            }
        }
    }
}