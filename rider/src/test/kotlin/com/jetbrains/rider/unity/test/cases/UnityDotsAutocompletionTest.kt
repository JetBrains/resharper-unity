package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.diagnostics.LogTraceScenarios
import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rider.unity.test.framework.api.waitForUnityPackagesCache
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity DOTS Autocompletion")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_7)
class UnityDotsAutocompletionTest : BaseTestWithSolution() {
    override val testSolution: String = "UnityDotsAutocompletionTestData"

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

    @Mute("RIDER-95438")
    @Test(description="DOTS Source GenCompletion")
    @ChecklistItems(["DOTS Source GenCompletion"])
    fun test_DotsSourceGenCompletion() {
        waitForRoslynReady()
        buildSolutionWithReSharperBuild()
        waitForUnityPackagesCache()
        withOpenedEditor("DotsUserStructures.cs") {
            waitForDaemon()
            waitForNextRoslynReady()
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