package com.jetbrains.rider.unity.test.cases.markup

import com.jetbrains.rider.unity.test.framework.api.doFindUsagesTest
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rd.ide.model.findUsagesHost
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.daemon.util.annotateDocumentWithHighlighterTags
import com.jetbrains.rdclient.daemon.util.backendAttributeIdOrThrow
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.executeWithGold
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@Mute("RIDER-114854")
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class InputSystemTest : BaseTestWithSolution() {
    override val testSolution: String = "InputSystemTestData"

    override fun preprocessTempDirectory(tempDir: File) {
        prepareAssemblies(activeSolutionDirectory)
    }

    override val traceCategories: List<String>
        get() = listOf(
            "#com.jetbrains.rdclient.document",
            "#com.jetbrains.rider.document",
            "#com.jetbrains.rider.editors",
            "#com.jetbrains.rdclient.daemon",
            "JetBrains.Rider.Test.Framework.Core.Documents",
            "JetBrains.ReSharper.Host.Features.Documents",
            "JetBrains.ReSharper.Host.Features.TextControls",
            "JetBrains.ReSharper.Psi.Caches")

    @BeforeMethod(alwaysRun = true)
    fun resetGroupings() {
        project.solution.findUsagesHost.groupingRules.valueOrNull?.items?.forEach { it.enabled.set(true) }
    }

    @Test
    fun findUsagesTest() {
        // PlayerInput is attached to Cube
        // NewBehaviourScript is attached Cube
        doFindUsagesTest("Assets/NewBehaviourScript.cs", "OnJump1")
    }

    @Test
    fun findUsagesWithPrefab1Test() {
        // Cube1 is a prefab
        // PlayerInput is attached to the Cube1 prefab
        // NewBehaviourScript1 is attached Cube1 on the scene
        doFindUsagesTest("Assets/NewBehaviourScript1.cs", "OnJump1WithPrefab")
    }

    @Test
    fun findUsagesWithPrefab2Test() {
        doFindUsagesTest("Assets/NewBehaviourScript2.cs", "OnJump1WithPrefab2")
    }

    @Test
    fun findUsagesWithPrefab3Test() {
        doFindUsagesTest("Assets/NewBehaviourScript3.cs", "OnJump1WithPrefab3")
    }

    @Test
    fun findUsagesWithPrefab4Test() {
        // Cube4 is a prefab
        // PlayerInput is attached to Cube4 on the scene
        // NewBehaviourScript is attached Cube4 on the prefab
        doFindUsagesTest("Assets/NewBehaviourScript4.cs", "OnJump1WithPrefab4")
    }

    @Test(enabled = false) // broadcast support is not yet implemented
    fun findUsagesBroadcastScriptTest() {
        doFindUsagesTest("Assets/BroadcastScript1.cs", "OnBroadcastScript1")
    }

    private fun doUsedCodeTest(relPath:String) {
        val projectLifetime = project.lifetime
        val model = project.solution.frontendBackendModel
        runSweaAndGetResults(project)
        waitAndPump(projectLifetime, { model.isDeferredCachesCompletedOnce.valueOrDefault(false) }, Duration.ofSeconds(10),
                    { "Deferred caches are not completed" })
        executeWithGold(testGoldFile) { ps ->
            withOpenedEditor(relPath) {
                // we call this here to ensure that we had IN_PROGRESS_GLOBAL followed by UP_TO_DATE
                waitForLenses() // todo: remove this hack, expect this would help RIDER-92327
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