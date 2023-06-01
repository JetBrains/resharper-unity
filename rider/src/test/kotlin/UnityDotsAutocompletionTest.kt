import base.integrationTests.prepareAssemblies
import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.test.waitForDaemon
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.time.Duration

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7)
class UnityDotsAutocompletionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String = "UnityDotsAutocompletionTestData"

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
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages",
            *LogTraceScenarios.Roslyn.categories.toTypedArray())

    @Test
    fun test_DotsSourceGenCompletion() {
        waitForRoslynReady()
        buildSolutionWithReSharperBuild()
        waitForUnityPackagesCache {
            withOpenedEditor("DotsUserStructures.cs") {
                waitForDaemon()
                setCaretAfterWord("//typing_position")
                startNewLine()

                callBasicCompletion()
                assertLookupNotContains("PublicStaticMethod")
                assertLookupNotContains("PublicVoidMethod")
                assertLookupNotContains("__codegen_PublicCodeGenMethod")
                assertLookupNotContains("UserAspect___userComponentData")
                assertLookupContains(
                    "StaticCodeGenClass", //to check if code gen works
                    "Entity",
                    "Foo",
                    "_userComponentData",
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

    private fun BaseTestWithSolution.waitForUnityPackagesCache(action: BaseTestWithSolution.() -> Unit): Unit {
        waitAndPump(project.lifetime,
                    { project.solution.frontendBackendModel.isUnityPackageManagerInitiallyIndexFinished.valueOrDefault(false) },
                    Duration.ofSeconds(10), { "Deferred caches are not completed" })
        action()
    }

}