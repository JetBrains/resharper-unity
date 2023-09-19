package com.jetbrains.rider.unity.test.cases
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.protocol.protocolHost
import com.jetbrains.rider.test.annotations.Mute
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class ProjectSettingsCompletionTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName(): String = "ProjectSettingsTestData"
    override fun preprocessTempDirectory(tempDir: File) {
        if (testMethod.name.contains("YamlOff")) {
            SettingsHelper.disableIsAssetIndexingEnabledSetting(activeSolution, activeSolutionDirectory)
        }
    }

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

    @Test
    fun testAnimatorState_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "AnimatorStateCompletionTest.cs") {
            typeWithLatency("\"")
            assertLookupContains("\"Alerted\"", checkFocus = false)
            assertLookupContains("\"AttackLoop\"", checkFocus = false)
        }
    }

    @Test
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

    val basicLayers = arrayOf(
        "\"Water\"",
        "\"Default\"",
        "\"UI\"",
        "\"Ignore Raycast\"",
        "\"PostProcessing\"",
        "\"TransparentFX\"")

    @Test
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

    @Test
    fun testLayer_PrimitiveCompletion_YamlOff() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }
    }

    @Test
    @Mute("RIDER-84785")
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
}