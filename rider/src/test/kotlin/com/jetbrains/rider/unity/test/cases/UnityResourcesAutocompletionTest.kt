package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
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
import com.jetbrains.rider.test.scriptingApi.assertLookupNotContains
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rider.unity.test.framework.api.waitForUnityPackagesCache
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity Resources Autocompletion")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("ResourcesAutocompletionTestData")
class UnityResourcesAutocompletionTest : PerTestSolutionTestBase() {
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

    @Test(description="Unity Resources Completion for Load")
    @ChecklistItems(["Unity Resources Completion/Load"])
    fun test_UnityResourcesLoadCompletion() {
        waitForUnityPackagesCache()
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
                "\"Folder\"",
                checkFocus = false)
        }
    }

    @Test(description="Unity Resources Completion for LoadAll")
    @ChecklistItems(["Unity Resources Completion/LoadAll"])
    fun test_UnityResourcesLoadAllCompletion() {
        waitForUnityPackagesCache()
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
                "\"Folder\"",
                checkFocus = false)
        }
    }

    @Test(description="Unity Resources Completion for LoadAsync")
    @ChecklistItems(["Unity Resources Completion/LoadAsync"])
    fun test_UnityResourcesLoadAsyncCompletion() {
        waitForUnityPackagesCache()
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
                "\"Folder\"",
                checkFocus = false)
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