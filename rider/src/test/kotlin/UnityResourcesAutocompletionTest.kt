
import base.integrationTests.prepareAssemblies
import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.assertLookupContains
import com.jetbrains.rider.test.scriptingApi.assertLookupNotContains
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class UnityResourcesAutocompletionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String = "ResourcesAutocompletionTestData"

    override val traceCategories: List<String>
        get() = listOf(
            "#com.jetbrains.rdclient.completion",
            "#com.jetbrains.rdclient.document",
            "#com.jetbrains.rider.document",
            "#com.jetbrains.rider.editors",
            "#com.jetbrains.rider.completion",
            "#com.jetbrains.rdclient.editorActions",
            "JetBrains.ReSharper.Host.Features.Completion",
            "JetBrains.Rider.Test.Framework.Core.Documents",
            "JetBrains.ReSharper.Feature.Services.CodeCompletion",
            "JetBrains.ReSharper.Host.Features.Completion.Strategies.CSharp",
            "JetBrains.ReSharper.Host.Features.Documents",
            "JetBrains.ReSharper.Host.Features.TextControls",
            "JetBrains.ReSharper.Psi.Caches",
            "JetBrains.ReSharper.Psi.Files",
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages")

    @Test
    fun test_UnityResourcesLoadCompletion() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "UnityResourcesLoadCompletion.cs") {
                typeWithLatency("\"")
                assertLookupNotContains("\"EscapeFromRider\"")
                assertLookupNotContains("\"ImpossibleResourceName\"")
                assertLookupContains(
                    "\"from_git__RUNTIME\"",
                    "\"from_git__EDITOR\"",
                    "\"from_disk__EDITOR\"",
                    "\"from_disk__RUNTIME\"",
                    "\"from_pack_folder__RUNTIME\"",
                    "\"from_pack_folder__EDITOR\"",
                    "\"from_res__RUNTIME\"",
                    "\"from_res__EDITOR\"",
                    checkFocus = false)
            }
        }
    }

    @Test
    fun test_UnityResourcesLoadAllCompletion() {
        waitForUnityPackagesCache() {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "UnityResourcesLoadAllCompletion.cs") {
                typeWithLatency("\"")
                assertLookupNotContains("\"EscapeFromRider\"")
                assertLookupNotContains("\"ImpossibleResourceName\"")
                assertLookupContains(
                    "\"from_git__RUNTIME\"",
                    "\"from_git__EDITOR\"",
                    "\"from_disk__EDITOR\"",
                    "\"from_disk__RUNTIME\"",
                    "\"from_pack_folder__RUNTIME\"",
                    "\"from_pack_folder__EDITOR\"",
                    "\"from_res__RUNTIME\"",
                    "\"from_res__EDITOR\"",
                    checkFocus = false)
            }
        }
    }

    @Test
    fun test_UnityResourcesLoadAsyncCompletion() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "UnityResourcesLoadAsyncCompletion.cs") {
                typeWithLatency("\"")
                assertLookupNotContains("\"EscapeFromRider\"")
                assertLookupNotContains("\"ImpossibleResourceName\"")
                assertLookupContains(
                    "\"from_git__RUNTIME\"",
                    "\"from_git__EDITOR\"",
                    "\"from_disk__EDITOR\"",
                    "\"from_disk__RUNTIME\"",
                    "\"from_pack_folder__RUNTIME\"",
                    "\"from_pack_folder__EDITOR\"",
                    "\"from_res__RUNTIME\"",
                    "\"from_res__EDITOR\"",
                    checkFocus = false)
            }
        }
    }

    @BeforeMethod
    fun initializeEnvironment() {
        TestModeFlags.set(CompletionAutoPopupHandler.ourTestingAutopopup, true)

        CodeInsightSettings.getInstance().COMPLETION_CASE_SENSITIVE = CodeInsightSettings.NONE
        CodeInsightSettings.getInstance().isSelectAutopopupSuggestionsByChars = true
        CodeInsightSettings.getInstance().AUTO_POPUP_JAVADOC_INFO = false

        //all tests were written with this setting which default was changed only in 18.3
        RiderCodeCompletionExtraSettings.instance.allowToCompleteWithWhitespace = true
        prepareAssemblies(project, activeSolutionDirectory)
    }

    // debug only
    @AfterMethod
    fun saveDocuments() {
        persistAllFilesOnDisk()
    }

    private fun BaseTestWithSolution.waitForUnityPackagesCache (action: BaseTestWithSolution.() -> Unit): Unit {
        waitAndPump(project.lifetime, { project.solution.frontendBackendModel.isUnityPackageManagerInitiallyIndexFinished.valueOrDefault(false) },
                    Duration.ofSeconds(10), { "Deferred caches are not completed" })
        action()
    }
}