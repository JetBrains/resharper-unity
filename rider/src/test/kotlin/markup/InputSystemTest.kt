import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.daemon.util.annotateDocumentWithHighlighterTags
import com.jetbrains.rdclient.daemon.util.backendAttributeIdOrThrow
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.time.Duration

class InputSystemTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "InputSystemTestData"
    }

    @Test
    fun unusedCodeTest() {
        val projectLifetime = project.lifetime
        val model = project.solution.frontendBackendModel
        waitAndPump(projectLifetime, { model.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })
        runSwea(project) // otherwise public methods are never marked unused
        executeWithGold(testGoldFile) { ps->
            ps.println("Should be used:")
            withOpenedEditor("Assets/Scripts/Mechanics/PlayerController.cs") {
                ps.print(annotateDocumentWithHighlighterTags(markupContributor.markupAdapter,
                                                             valueFilter = { it.backendAttributeIdOrThrow == "ReSharper Dead Code" }))
            }
            ps.println("Should be unused:")
            withOpenedEditor("Assets/Scenes/SampleScene.unity") {
                this.setCaretAfterWord("m_MethodName: OnJump")
                this.typeWithLatency("2")
            }
            waitAndPump(projectLifetime, { model.isDeferredCachesCompletedOnce.valueOrDefault(false)}, Duration.ofSeconds(10), { "Deferred caches are not completed" })

            withOpenedEditor("Assets/Scripts/Mechanics/PlayerController.cs") {
                ps.print(annotateDocumentWithHighlighterTags(markupContributor.markupAdapter, valueFilter = { it.backendAttributeIdOrThrow == "ReSharper Dead Code" }))
            }
        }
    }
}