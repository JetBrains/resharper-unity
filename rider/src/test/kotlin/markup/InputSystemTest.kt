import base.integrationTests.prepareAssemblies
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.daemon.util.annotateDocumentWithHighlighterTags
import com.jetbrains.rdclient.daemon.util.backendAttributeIdOrThrow
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.markupContributor
import com.jetbrains.rider.test.scriptingApi.runSwea
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.Test
import java.io.File
import java.time.Duration


class InputSystemTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String {
        return "InputSystemTestData"
    }

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
    }

    private fun doUsedCodeTest(fileName:String) {
        val projectLifetime = project.lifetime
        val model = project.solution.frontendBackendModel
        runSwea(project) // otherwise public methods are never marked unused
        waitAndPump(projectLifetime, { model.isDeferredCachesCompletedOnce.valueOrDefault(false) }, Duration.ofSeconds(10),
                    { "Deferred caches are not completed" })
        executeWithGold(testGoldFile) { ps ->
            withOpenedEditor(fileName) {
                ps.println(annotateDocumentWithHighlighterTags(markupContributor.markupAdapter,
                                                               valueFilter = { it.backendAttributeIdOrThrow == "ReSharper Dead Code" }))
            }
        }
    }

    @Test
    fun usedCodeTest() {
        // PlayerInput is attached to Cube
        // NewBehaviourScript is attached Cube
        doUsedCodeTest("Assets/NewBehaviourScript.cs")
    }

    @Test
    fun usedCodeTestWithPrefab1() {
        // Cube1 is a prefab
        // PlayerInput is attached to the Cube1 prefab
        // NewBehaviourScript is attached Cube1 on the scene
        doUsedCodeTest("Assets/NewBehaviourScript1.cs")
    }

    @Test
    fun usedCodeTestWithPrefab2() {
        // Cube2 is a prefab, but everything is attached on the scene:
        // PlayerInput is attached to Cube2 on the scene
        // NewBehaviourScript is attached Cube2 on the scene
        doUsedCodeTest("Assets/NewBehaviourScript2.cs")
    }

    @Test
    fun usedCodeTestWithPrefab3() {
        // Cube3 is a prefab, everything is attached to the prefab:
        // PlayerInput is attached to Cube2 on the prefab
        // NewBehaviourScript is attached Cube2 on the prefab
        doUsedCodeTest("Assets/NewBehaviourScript3.cs")
    }

    @Test
    fun usedCodeTestWithPrefab4() {
        // Cube4 is a prefab
        // PlayerInput is attached to Cube4 on the scene
        // NewBehaviourScript is attached Cube4 on the prefab
        doUsedCodeTest("Assets/NewBehaviourScript4.cs")
    }

    @Test
    fun usedCodeTestBroadcastScript1() {
        // PlayerInput is attached to root (Cube)
        // BroadcastScript1 is attached to child (Cube2)
        // PlayerInput behaviour is Broadcast
        doUsedCodeTest("Assets/BroadcastScript1.cs")
    }
}