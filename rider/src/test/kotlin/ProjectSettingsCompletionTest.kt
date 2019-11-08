import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.protocol.protocolHost
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

class ProjectSettingsCompletionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String = "ProjectSettingsTestData"

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

    @Test(enabled = false)
    fun testScene_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "SceneCompletionTest.cs") {
            typeWithLatency("\"")
            assertLookupNotContains("\"ImpossibleShortName\"")
            assertLookupNotContains("\"ImpossibleShortName2\"")
            assertLookupContains(
                "\"PossibleShortName\"",
                "\"Scenes/PossibleShortName\"",
                "\"Scenes/ImpossibleShortName\"",
                "\"Scenes/ImpossibleShortName2\"",
                "\"Scenes/Folder/ImpossibleShortName\"",
                checkFocus = false)
        }
    }

    @Test(enabled = false)
    fun testInput_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "InputCompletionTest.cs") {
            typeWithLatency("\"")
            assertLookupContains(
                "\"Jump\"",
                "\"Fire1\"",
                "\"Fire2\"",
                "\"Fire3\"",
                "\"Cancel\"",
                "\"Submit\"",
                "\"Vertical\"",
                "\"Horizontal\"",
                "\"Mouse X\"",
                "\"Mouse Y\"",
                "\"Mouse ScrollWheel\"",
                checkFocus = false)
        }
    }

    val basicTags = arrayOf(
        "\"Finish\"",
        "\"Player\"",
        "\"Respawn\"",
        "\"Untagged\"",
        "\"EditorOnly\"",
        "\"MainCamera\"",
        "\"GameController\"")

    @Test(enabled = false)
    fun testTag_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest2.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest3.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
        }
    }

    val basicLayers = arrayOf(
        "\"Water\"",
        "\"Default\"",
        "\"UI\"",
        "\"Ignore Raycast\"",
        "\"PostProcessing\"",
        "\"TransparentFX\"")

    @Test(enabled = false)
    fun testLayer_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest2.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }
    }

    @Test(enabled = false)
    fun testLayer_CompletionAfterModification() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest2.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }

        replaceFileContent(project, File("ProjectSettings").resolve("TagManager.asset").path, "TagManager.asset")
        TestHost.getInstance(project.protocolHost).backendWaitForCaches("waitForAllAnalysisFinished")

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, "\"Test1\"", checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest2.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, "\"Test1\"", checkFocus = false)
        }
    }

    @BeforeMethod
    fun InitializeEnvironement() {
        TestModeFlags.set(CompletionAutoPopupHandler.ourTestingAutopopup, true)

        CodeInsightSettings.getInstance().COMPLETION_CASE_SENSITIVE = CodeInsightSettings.NONE
        CodeInsightSettings.getInstance().isSelectAutopopupSuggestionsByChars = true
        CodeInsightSettings.getInstance().AUTO_POPUP_JAVADOC_INFO = false

        //all tests were written with this setting which default was changed only in 18.3
        RiderCodeCompletionExtraSettings.instance.allowToCompleteWithWhitespace = true
        CopyUnityDll(project, activeSolutionDirectory)
    }

    // debug only
    @AfterMethod
    fun SaveDocuments() {
        persistAllFilesOnDisk(project)
    }
}