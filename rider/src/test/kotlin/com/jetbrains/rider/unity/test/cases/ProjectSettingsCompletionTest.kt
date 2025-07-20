package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rdclient.client.frontendProjectSession
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.SettingsHelper
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity project settings completion")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.LATEST_STABLE)
@Solution("ProjectSettingsTestData")
class ProjectSettingsCompletionTest : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = {
            if (testMethod.name.contains("YamlOff")) {
                SettingsHelper.disableIsAssetIndexingEnabledSetting(it.name, it)
            }
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

    @Test(description = "Test scene primitive completion")
    @ChecklistItems(["Project Settings Completion/Scene primitive"])
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

    @Test(description = "Test Animator state primitive completion")
    @ChecklistItems(["Project Settings Completion/Animator state primitive"])
    fun testAnimatorState_PrimitiveCompletion() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "AnimatorStateCompletionTest.cs") {
            typeWithLatency("\"")
            assertLookupContains("\"Alerted\"", checkFocus = false)
            assertLookupContains("\"AttackLoop\"", checkFocus = false)
        }
    }

    @Test(description = "Test Input primitive completion")
    @ChecklistItems(["Project Settings Completion/Input primitive"])
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

    @Test(description = "Test Layer primitive completion")
    @ChecklistItems(["Project Settings Completion/Layer primitive"])
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

    @Test(description = "Test Layer primitive completion with turned off Yaml")
    @ChecklistItems(["Project Settings Completion/Layer primitive with turned off Yaml"])
    fun testLayer_PrimitiveCompletion_YamlOff() {
        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "LayerCompletionTest1.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicLayers, checkFocus = false)
        }
    }

    @Test(description = "Test Layer completion after modification")
    @Mute("RIDER-84785")
    @ChecklistItems(["Project Settings Completion/Layer completion after modification"])
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
        TestHost.getInstance(project.frontendProjectSession.appSession).backendWaitForCaches("waitForAllAnalysisFinished")

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

        CodeInsightSettings.getInstance().completionCaseSensitive = CodeInsightSettings.NONE
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