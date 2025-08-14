package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.assertLookupContains
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity Tags Autocompletion")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("ProjectSettingsTestData")
class TagsCompletionTest : PerTestSolutionTestBase() {
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

    @Suppress("NonAsciiCharacters")
    val basicTags = arrayOf(
        "\"Finish\"",
        "\"Player\"",
        "\"Respawn\"",
        "\"Untagged\"",
        "\"EditorOnly\"",
        "\"MainCamera\"",
        "\"GameController\"",
        "\"ABC\"",
        "\"Würzburg\"")

    @Test(description="Check completion for basic tags (Finish, PLayer, Respawn, EditorOnly and etc.)")
    @ChecklistItems(["Tags Completion/Basic tags (Finish, Player, Respawn, EditorOnly)"])
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

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest4.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest5.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
        }

        withOpenedEditor(File("Assets").resolve("NewBehaviourScript.cs").path, "TagCompletionTest6.cs") {
            typeWithLatency("\"")
            assertLookupContains(*basicTags, checkFocus = false)
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