import base.integrationTests.prepareAssemblies
import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_6)
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
            "JetBrains.ReSharper.Psi.Files")

    @Test
    fun test_UnityResourcesLoadCompletion() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "UnityResourcesLoadCompletion.cs") {
                typeWithLatency("\"")
                assertLookupNotContains("\"EscapeFromRider\"")
                assertLookupNotContains("\"ImpossibleResourceName\"")
                assertLookupContains(
                    "\"resources_package_from_git_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_git_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR 1\"",
                    "\"Assets_Resources_Asset__RUNTIME\"",
                    "\"Assets_Resources_Items_Resources_Asset__RUNTIME\"",
                    "\"Editor/Assets_Resources_Editor_Asset__RUNTIME\"",
                    "\"Editor/Resource/Assets_Resources_Editor_Resources_Asset__RUNTIME\"",
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
                    "\"resources_package_from_git_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_git_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR 1\"",
                    "\"Assets_Resources_Asset__RUNTIME\"",
                    "\"Assets_Resources_Items_Resources_Asset__RUNTIME\"",
                    "\"Editor/Assets_Resources_Editor_Asset__RUNTIME\"",
                    "\"Editor/Resource/Assets_Resources_Editor_Resources_Asset__RUNTIME\"",
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
                    "\"resources_package_from_git_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_git_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Editor_Resources_Asset__EDITOR\"",
                    "\"resources_package_from_disk_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Runtime_Resources_Asset__RUNTIME\"",
                    "\"resources_package_from_packages_folder_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR\"",
                    "\"Assets_Editor_Resources_Asset__EDITOR 1\"",
                    "\"Assets_Resources_Asset__RUNTIME\"",
                    "\"Assets_Resources_Items_Resources_Asset__RUNTIME\"",
                    "\"Editor/Assets_Resources_Editor_Asset__RUNTIME\"",
                    "\"Editor/Resource/Assets_Resources_Editor_Resources_Asset__RUNTIME\"",
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